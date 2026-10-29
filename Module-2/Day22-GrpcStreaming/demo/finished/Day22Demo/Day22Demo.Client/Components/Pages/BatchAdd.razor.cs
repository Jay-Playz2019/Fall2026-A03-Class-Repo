using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day22Demo.Client.Components.Pages;

public partial class BatchAdd : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private bool IsSending { get; set; }
    private string? StatusMessage { get; set; }
    private string? ErrorMessage { get; set; }

    private List<(string Name, double Price, ProductCategory Category)> NewProducts =
    [
        ("Mechanical Keyboard", 89.99, ProductCategory.Electronics),
        ("Clean Code (Book)", 39.99, ProductCategory.Books),
        ("Campus Water Bottle", 15.00, ProductCategory.Supplies)
    ];

    private async Task BatchAddAsync()
    {
        try
        {
            IsSending = true;
            StatusMessage = null;
            ErrorMessage = null;

            // Opens the upstream — note there's NO request argument, because the data
            // goes up the RequestStream, not in the call. We get back a call exposing a
            // RequestStream to write into plus a single awaitable response.
            using var call = ProductClient.BatchAddProducts();

            foreach (var item in NewProducts)
            {
                // Push each item up the SAME open connection — no new request or
                // handshake per item, which is the whole point of client streaming
                // versus firing N separate unary calls.
                await call.RequestStream.WriteAsync(new AddProductRequest
                {
                    Name = item.Name,
                    Price = item.Price,
                    Category = item.Category
                });
            }

            // CompleteAsync is mandatory: it sends the end-of-stream marker that lets the
            // server's `await foreach` finish. Skip it and the server blocks forever
            // waiting for more items — and the `await call` below never returns.
            await call.RequestStream.CompleteAsync();
            // The client-streaming asymmetry: many writes up, exactly ONE response back,
            // and it only arrives after CompleteAsync.
            var response = await call;
            StatusMessage = $"Added {response.ProductsAdded} products.";
        }
        catch (RpcException ex)
        {
            ErrorMessage = $"Batch add error: {ex.Status.Detail}";
        }
        finally
        {
            IsSending = false;
        }
    }
}
