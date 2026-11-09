using CampusStore.Protos;
using Day23Demo.Client.Auth;
using Grpc.Core;
using Grpc.Net.Client.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();

// DAY23_GRPC_SERVER_URL overrides the server address when set (used by the
// /verify-demo test harness to point at a recorder-spawned server on plain
// HTTP). Default is the production-shaped HTTPS URL for Visual Studio F5.
var grpcServerUrl = builder.Configuration["DAY23_GRPC_SERVER_URL"]
    ?? "https://localhost:7009";
var isInsecureGrpc = grpcServerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
builder.Services.AddGrpcClient<ProductCatalog.ProductCatalogClient>(options =>
{
    options.Address = new Uri(grpcServerUrl);
})
// Channel settings: the retry policy lives here
.ConfigureChannel(options =>
{
    // AddCallCredentials defaults to TLS-only to prevent bearer tokens from
    // flowing over an insecure transport. Allow plain-HTTP ONLY when the
    // DAY23_GRPC_SERVER_URL override points at http:// — a test-harness
    // escape hatch for verify-demo. In production the default HTTPS path
    // is used and this flag stays off.
    if (isInsecureGrpc)
    {
        options.UnsafeUseInsecureChannelCallCredentials = true;
    }
    // RESILIENCE, configured once at the channel — not coded into every call site.
    // The gRPC client library retries transparently per this policy; the page code
    // (ProductLookup) never sees the failed attempts, only the final outcome.
    options.ServiceConfig = new ServiceConfig
    {
        // One entry per group of methods
        MethodConfigs =
        {
            new MethodConfig
            {
                // MethodName.Default = apply this policy to EVERY method on the
                // channel (you could instead name a specific RPC to scope it).
                Names = { MethodName.Default },
                // How to retry those methods
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,                                  // 1 original + up to 2 retries
                    InitialBackoff = TimeSpan.FromMilliseconds(500),  // wait before retry #1
                    MaxBackoff = TimeSpan.FromSeconds(5),             // backoff ceiling
                    BackoffMultiplier = 2,                            // exponential: 500ms → 1s → 2s…
                    // ONLY retry Unavailable (server down / transient network).
                    // Deliberately NOT NotFound or InvalidArgument — retrying a
                    // deterministic failure just fails again 3× and delays the error.
                    // Retries also assume the call is idempotent (safe to repeat).
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            }
        }
    };
})
.AddCallCredentials(async (context, metadata, serviceProvider) =>
{
    var tokenProvider = serviceProvider.GetRequiredService<ITokenProvider>();
    var token = await tokenProvider.GetTokenAsync();
    if (!string.IsNullOrEmpty(token))
    {
        metadata.Add("Authorization", $"Bearer {token}");
    }
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Day23Demo.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
