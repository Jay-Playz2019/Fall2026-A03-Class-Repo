using CampusStore.Protos;
using Day20Demo.Client.Auth;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ITokenProvider, DemoTokenProvider>();

builder.Services.AddGrpcClient<ProductCatalog.ProductCatalogClient>(options =>
{
    options.Address = new Uri("https://localhost:7001");
})
.AddCallCredentials(async (context, metadata, serviceProvider) =>
{
    // Resolve the token provider from DI
    var tokenProvider = serviceProvider.GetRequiredService<ITokenProvider>();
    var token = await tokenProvider.GetTokenAsync();
    if (!string.IsNullOrEmpty(token))
    {
        // gRPC metadata is the header: Authorization: Bearer ...
        metadata.Add("Authorization", $"Bearer {token}");
    }
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Day20Demo.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
