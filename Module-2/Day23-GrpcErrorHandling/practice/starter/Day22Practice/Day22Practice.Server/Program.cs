using Day22Practice.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// NOTE (carry-over from Day 21, not part of today's practice): these JWT values
//       belong in appsettings.Development.json, read via IConfiguration.
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
                Encoding.UTF8.GetBytes("ThisIsADemoSigningKeyForDay22AtLeast32Chars!"))
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "admin"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<OrderManagementService>();

app.Run();
