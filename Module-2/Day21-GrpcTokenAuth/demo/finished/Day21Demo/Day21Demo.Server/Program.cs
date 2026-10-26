using Day21Demo.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// The server side of Day-20's bearer token. Day-20's client ATTACHED a token;
// today the server VALIDATES it. AddJwtBearer rejects any call whose token is
// missing, tampered, expired, or from the wrong issuer/audience — before the
// gRPC method ever runs. Same JWT middleware students wired for Blazor pages on
// Day-13, just guarding gRPC endpoints instead.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT short claim names ("role", "name", "email") as-is.
        // Default behaviour remaps them to long Microsoft URI claim types
        // (e.g. http://schemas.microsoft.com/ws/2008/06/identity/claims/role),
        // which breaks policy.RequireClaim("role", "admin") because the
        // policy matches the Type field literally. See the reference sheet
        // Gotcha section and Shared-Resources/Reference-Materials/blazor-gotchas.md Section 2.1.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://demo-issuer.example.com",
            ValidateAudience = true,
            ValidAudience = "grpc-demo-api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // No grace period: an expired token is rejected immediately (default skew is 5 min).
            ValidateIssuerSigningKey = true,
            // The signing key MUST match the one that minted the token (here,
            // TokenHelper / the client's JwtTokenProvider). A mismatch fails
            // ValidateIssuerSigningKey and every call is rejected as Unauthenticated.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("ThisIsADemoSigningKeyForDay21AtLeast32Chars!"))
        };
    });
// A named authorization policy: "AdminOnly" is satisfied only by a token carrying
// a role=admin claim. The [Authorize(Policy = "AdminOnly")] attribute on a method
// references this by name — same role/policy model as Day-16, now on the gRPC server.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "admin"));

var app = builder.Build();

// Order is load-bearing: UseAuthentication (work out WHO the caller is from the
// token) must run BEFORE UseAuthorization (decide whether they're ALLOWED). Reversed,
// authorization runs against an anonymous user and every [Authorize] call fails.
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ProductCatalogService>();

app.Run();
