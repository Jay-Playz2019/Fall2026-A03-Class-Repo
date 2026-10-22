using CampusStore.Protos;
using Day20Practice.Client.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Practice 3: Register the token source. AddCallCredentials resolves it from DI
// per call, so the call sites never change when this becomes a real one.
builder.Services.AddScoped<ITokenProvider, DemoTokenProvider>();

// Practice 1: Register the typed gRPC client with the server address — the client
// mirror of Day-19's server registration, and the same DI shape as AddHttpClient.
// OrderManagementClient is generated from order.proto, so the call sites are typed.
// Practice 3: Chain AddCallCredentials — a per-call callback the channel fires once
// per outbound call to attach the bearer token, so a refreshed token is picked up
// transparently with no per-call-site header code.
builder.Services.AddGrpcClient<OrderManagement.OrderManagementClient>(options =>
{
    options.Address = new Uri("https://localhost:7003");
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

app.MapRazorComponents<Day20Practice.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
