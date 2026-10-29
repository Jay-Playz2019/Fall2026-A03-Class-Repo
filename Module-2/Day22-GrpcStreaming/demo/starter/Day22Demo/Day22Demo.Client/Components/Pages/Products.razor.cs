using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day22Demo.Client.Components.Pages;

public partial class Products : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private List<Product> ProductList { get; set; } = [];
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await ProductClient.ListProductsAsync(
                new ListProductsRequest());
            ProductList = [.. response.Products];
        }
        catch (RpcException ex)
        {
            ErrorMessage = ex.StatusCode switch
            {
                StatusCode.Unavailable => "Product service is currently unavailable.",
                StatusCode.Unauthenticated => "Authentication required. Check your token.",
                StatusCode.PermissionDenied => "You don't have permission for this action.",
                _ => $"Error: {ex.Status.Detail}"
            };
        }
    }
}
