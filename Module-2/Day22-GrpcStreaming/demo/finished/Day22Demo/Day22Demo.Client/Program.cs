using CampusStore.Protos;
using Day22Demo.Client.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();

// DAY22_GRPC_SERVER_URL overrides the server address when set (used by the
// /verify-demo test harness to point at a recorder-spawned server on plain
// HTTP). Default is the production-shaped HTTPS URL for Visual Studio F5.
var grpcServerUrl = builder.Configuration["DAY22_GRPC_SERVER_URL"]
    ?? "https://localhost:7005";
var isInsecureGrpc = grpcServerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
builder.Services.AddGrpcClient<ProductCatalog.ProductCatalogClient>(options =>
{
    options.Address = new Uri(grpcServerUrl);
})
.ConfigureChannel(channelOptions =>
{
    // AddCallCredentials defaults to TLS-only to prevent bearer tokens from
    // flowing over an insecure transport. Allow plain-HTTP ONLY when the
    // DAY22_GRPC_SERVER_URL override points at http:// — a test-harness
    // escape hatch for verify-demo. In production the default HTTPS path
    // is used and this flag stays off.
    if (isInsecureGrpc)
    {
        channelOptions.UnsafeUseInsecureChannelCallCredentials = true;
    }
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

app.MapRazorComponents<Day22Demo.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
