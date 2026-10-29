using CampusStore.Protos;
using Day22Practice.Client.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();

builder.Services.AddGrpcClient<OrderManagement.OrderManagementClient>(options =>
{
    options.Address = new Uri("https://localhost:7007");
})
// gRPC attaches call credentials only over TLS by default — bearer tokens never
// flow over plain HTTP. The default https:// address above satisfies this.
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

app.MapRazorComponents<Day22Practice.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
