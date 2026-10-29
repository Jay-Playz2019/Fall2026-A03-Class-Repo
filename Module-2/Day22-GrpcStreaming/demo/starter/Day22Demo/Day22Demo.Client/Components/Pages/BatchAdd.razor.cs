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
        await Task.CompletedTask;
    }
}
