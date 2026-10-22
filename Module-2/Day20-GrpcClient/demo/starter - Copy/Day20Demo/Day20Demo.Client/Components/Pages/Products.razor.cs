using Grpc.Core;
using Microsoft.AspNetCore.Components;
// The generated classes, in the namespace set by option csharp_namespace
using CampusStore.Protos;

namespace Day20Demo.Client.Components.Pages;

public partial class Products : ComponentBase
{
    // Inject the generated client that AddGrpcClient registered
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private List<Product> ProductList { get; set; } = [];
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // A typed RPC call: no URL, no JSON
            var response = await ProductClient.ListProductsAsync(
                new ListProductsRequest());
            // Spread the repeated field into a List for binding
            ProductList = [.. response.Products];
        }
        // Every gRPC failure arrives as one RpcException
        catch (RpcException ex)
        {
            // Map the status code to a friendly message
            ErrorMessage = ex.StatusCode switch
            {
                StatusCode.Unavailable => "Product service is currently unavailable.",
                _ => $"Error: {ex.Status.Detail}"
            };
        }
    }
}