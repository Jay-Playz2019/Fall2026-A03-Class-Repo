using CampusStore.Protos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Practice 1: Register the typed gRPC client with the server address — the client
// mirror of Day-19's server registration, and the same DI shape as AddHttpClient.
// OrderManagementClient is generated from order.proto, so the call sites are typed.
builder.Services.AddGrpcClient<OrderManagement.OrderManagementClient>(options =>
{
    options.Address = new Uri("https://localhost:7003");
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Day20Practice.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
