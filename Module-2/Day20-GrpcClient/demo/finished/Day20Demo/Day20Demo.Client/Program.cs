using CampusStore.Protos;
using Day20Demo.Client.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register the token provider for bearer auth. The AddCallCredentials callback
// below resolves this from DI on every call, so swapping DemoTokenProvider for a
// real provider-backed implementation changes nothing at the call sites.
builder.Services.AddScoped<ITokenProvider, DemoTokenProvider>();

// Register the typed gRPC client into DI. This is the client mirror of Day-19's
// server-side AddGrpc()/MapGrpcService<T>(): same DI shape students used for
// AddHttpClient in REST, but ProductCatalogClient is GENERATED from the .proto,
// so it knows every RPC and message type at compile time (no URL strings, no
// hand-written JSON). options.Address = where the server listens.
// Register gRPC client with bearer token credentials.
// DAY20_GRPC_SERVER_URL overrides the server address when set (used by the
// /verify-demo test harness to point at a recorder-spawned server on plain
// HTTP). Default is the production-shaped HTTPS URL for Visual Studio F5.
var grpcServerUrl = builder.Configuration["DAY20_GRPC_SERVER_URL"]
    ?? "https://localhost:7001";
var isInsecureGrpc = grpcServerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
builder.Services.AddGrpcClient<ProductCatalog.ProductCatalogClient>(options =>
{
    options.Address = new Uri(grpcServerUrl);
})
.ConfigureChannel(channelOptions =>
{
    // AddCallCredentials defaults to TLS-only to prevent bearer tokens from
    // flowing over an insecure transport. Allow plain-HTTP ONLY when the
    // DAY20_GRPC_SERVER_URL override points at http:// — a test-harness
    // escape hatch for verify-demo. In production the default HTTPS path
    // is used and this flag stays off.
    if (isInsecureGrpc)
    {
        channelOptions.UnsafeUseInsecureChannelCallCredentials = true;
    }
})
// AddCallCredentials is the whole point of today: it attaches the bearer token
// to every outbound call. The channel fires this callback ONCE PER CALL, so a
// refreshed token is picked up transparently — no per-call-site header code, and
// no token baked in at startup. It resolves ITokenProvider from DI, awaits the
// token, and writes it into the call's outbound metadata as an Authorization header.
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

app.MapRazorComponents<Day20Demo.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
