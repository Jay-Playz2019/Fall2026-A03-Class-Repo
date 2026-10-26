using Day21Practice.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// Practice 1: validate the bearer token the client attached on Day-20. Issuer,
// audience, lifetime, and signing key must all match how the token was minted, or
// the call is rejected as Unauthenticated before reaching the service.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT short claim names ("role", "name", "email") as-is — otherwise the default
        // remap to long Microsoft URI claim types breaks policy.RequireClaim("role", "admin").
        // See Shared-Resources/Reference-Materials/blazor-gotchas.md Section 2.1.
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("ThisIsADemoSigningKeyForDay21AtLeast32Chars!"))
        };
    });
builder.Services.AddAuthorizationBuilder();

var app = builder.Build();

// Practice 1: order is load-bearing — authenticate (who is the caller) before
// authorize (are they allowed). Reversed, every [Authorize] runs against an
// anonymous user and fails.
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<OrderManagementService>();

app.Run();
