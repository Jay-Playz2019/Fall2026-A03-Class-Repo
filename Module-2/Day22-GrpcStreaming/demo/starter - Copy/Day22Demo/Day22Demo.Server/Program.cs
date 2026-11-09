using Day22Demo.Server;
using Day22Demo.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// TokenHelper reads Jwt:* values from appsettings / appsettings.Development.json
builder.Services.AddSingleton<TokenHelper>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT short claim names ("role", "name", "email") as-is.
        // Default behaviour remaps them to long Microsoft URI claim types,
        // which silently breaks policy.RequireClaim("role", "admin").
        // See Day 21 reference sheet and
        // Shared-Resources/Reference-Materials/blazor-gotchas.md Section 2.1.
        options.MapInboundClaims = false;
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig["Issuer"] ?? "https://demo-issuer.example.com",
            ValidateAudience = true,
            ValidAudience = jwtConfig["Audience"] ?? "grpc-demo-api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // No grace period: an expired token is rejected immediately (default skew is 5 min).
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig["SigningKey"]
                    ?? throw new InvalidOperationException("Jwt:SigningKey not configured")))
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
