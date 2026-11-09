using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day22Demo.Client.Components.Pages;

public partial class ProductStream : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private List<Product> ProductList { get; set; } = [];
    private bool IsStreaming { get; set; }
    private string? ErrorMessage { get; set; }

    private async Task LoadStreamAsync()
    {
        await Task.CompletedTask;
    }
}
