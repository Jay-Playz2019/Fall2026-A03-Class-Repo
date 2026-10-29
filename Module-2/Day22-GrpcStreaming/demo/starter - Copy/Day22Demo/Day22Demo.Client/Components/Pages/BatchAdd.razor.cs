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

    // Pre-built sample data for the demo
    private List<(string Name, double Price, ProductCategory Category)> NewProducts =
    [
        ("Mechanical Keyboard", 89.99, ProductCategory.Electronics),
        ("Clean Code (Book)", 39.99, ProductCategory.Books),
        ("Campus Water Bottle", 15.00, ProductCategory.Supplies)
    ];

    private async Task BatchAddAsync()
    {
        using var call = ProductClient.BatchAddProducts();

        foreach (var item in NewProducts)
        {
            // Send one item up the same connection
            await call.RequestStream.WriteAsync(new AddProductRequest
            {
                Name = item.Name,
                Price = item.Price,
                Category = item.Category
            });
        }

        // Tell the server the client is done sending
        await call.RequestStream.CompleteAsync();
        // The single response arrives after CompleteAsync
        var response = await call;
        StatusMessage = $"Added {response.ProductsAdded} products.";
    }
}
