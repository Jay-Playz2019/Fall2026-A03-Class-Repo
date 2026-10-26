using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day21Demo.Client.Components.Pages;

public partial class Products : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private List<Product> ProductList { get; set; } = [];
    private string? ErrorMessage { get; set; }
    private string? CallResult { get; set; }

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
            ErrorMessage = Describe(ex);
        }
    }

    // PRE-BUILT test call (do not edit).
    private async Task GetProductOneAsync()
    {
        try
        {
            var response = await ProductClient.GetProductAsync(new GetProductRequest { Id = 1 });
            CallResult = $"GetProduct succeeded: {response.Product.Name}.";
        }
        catch (RpcException ex)
        {
            CallResult = $"GetProduct failed ({ex.StatusCode}): {Describe(ex)}";
        }
    }

    private static string Describe(RpcException ex) => ex.StatusCode switch
    {
        StatusCode.Unavailable => "Product service is currently unavailable.",
        StatusCode.Unauthenticated => "Authentication required. Check your token.",
        StatusCode.PermissionDenied => "You don't have permission for this action.",
        _ => $"Error: {ex.Status.Detail}"
    };
}
