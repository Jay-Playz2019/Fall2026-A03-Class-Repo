using Day23Demo.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Required so the JWT "role" claim is not remapped to the long Microsoft URI form.
        // Without this, policies like RequireClaim("role", "admin") silently fail against
        // valid admin tokens. See Shared-Resources/Reference-Materials/blazor-gotchas.md Section 2.1.
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
                Encoding.UTF8.GetBytes("ThisIsADemoSigningKeyForDay23AtLeast32Chars!"))
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "admin"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ProductCatalogService>();

app.Run();
