using Day23Demo.Server.Interceptors;
using Day23Demo.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// TEST HARNESS — NOT TEACHING CODE.
// When DAY23_INJECT_TRANSIENT_FAILURES=1, register a server interceptor
// that fails the first N unary calls with Unavailable. This lets
// /verify-demo exercise the client-side retry policy end-to-end. The
// interceptor is inert in every other runtime path. See
// Interceptors/TransientFailureInterceptor.cs for the full rationale.
var injectTransientFailures =
    builder.Configuration["DAY23_INJECT_TRANSIENT_FAILURES"] == "1";
if (injectTransientFailures)
{
    builder.Services.AddSingleton<TransientFailureInterceptor>();
    builder.Services.AddGrpc(options =>
    {
        options.Interceptors.Add<TransientFailureInterceptor>();
    });
}
else
{
    builder.Services.AddGrpc();
}

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
