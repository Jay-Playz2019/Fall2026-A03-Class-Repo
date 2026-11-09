using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day23Demo.Client.Components.Pages;

public partial class ProductLookup : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private int ProductId { get; set; } = 1;
    private Product? SelectedProduct { get; set; }
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    private async Task LookupProduct()
    {
        ErrorMessage = null;
        SelectedProduct = null;
        IsLoading = true;

        try
        {
            var response = await ProductClient.GetProductAsync(
                new GetProductRequest { Id = ProductId });
            SelectedProduct = response.Product;
        }
        catch (RpcException ex)
        {
            ErrorMessage = $"Error: {ex.Status.Detail}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
