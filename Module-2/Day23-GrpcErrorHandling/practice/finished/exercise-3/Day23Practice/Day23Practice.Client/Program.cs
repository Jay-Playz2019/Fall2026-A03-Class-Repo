using CampusStore.Protos;
using Day23Practice.Client.Auth;
using Grpc.Core;
using Grpc.Net.Client.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();

builder.Services.AddGrpcClient<OrderManagement.OrderManagementClient>(options =>
{
    options.Address = new Uri("https://localhost:7011");
})
// Channel settings: the retry policy lives here
.ConfigureChannel(options =>
{
    // Practice 3: resilience configured ONCE at the channel — the client retries
    // transparently and the page code never sees the failed attempts.
    options.ServiceConfig = new ServiceConfig
    {
        // One entry per group of methods
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },   // apply to every method on the channel
                // How to retry those methods
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,                                  // 1 original + 2 retries
                    // Wait before the first retry
                    InitialBackoff = TimeSpan.FromMilliseconds(500),
                    // Never wait longer than this
                    MaxBackoff = TimeSpan.FromSeconds(5),
                    BackoffMultiplier = 2,                            // 500ms → 1s → 2s…
                    // Only Unavailable (transient). Retrying NotFound/InvalidArgument
                    // would just fail again 3× and delay the error.
                    // Retries assume the call is idempotent -- and MethodName.Default above
                    // applies this policy to EVERY method on the channel, including
                    // CreateOrder, which is NOT idempotent. That is a deliberate teaching
                    // simplification for a 10-minute practice. In production, scope the
                    // policy to named read methods instead (see the walkthrough's "Key
                    // decisions"). Do not carry MethodName.Default into Lab 2.
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

app.MapRazorComponents<Day23Practice.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
