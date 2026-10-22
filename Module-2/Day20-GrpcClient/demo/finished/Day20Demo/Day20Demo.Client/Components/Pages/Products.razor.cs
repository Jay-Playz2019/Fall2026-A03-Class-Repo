using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day20Demo.Client.Components.Pages;

public partial class Products : ComponentBase
{
    // The generated typed client — injected like any service because Program.cs
    // registered it with AddGrpcClient<T>(). It is NOT hand-written: Grpc.Tools
    // emits it from product.proto, so the method names and request/response types
    // below are the contract itself, checked at compile time.
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private List<Product> ProductList { get; set; } = [];
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // A strongly-typed RPC, not HttpClient.GetAsync("/api/products"): no
            // URL, no manual JSON. response.Products is the proto's repeated field,
            // spread into a List for binding.
            var response = await ProductClient.ListProductsAsync(
                new ListProductsRequest());
            ProductList = [.. response.Products];
        }
        catch (RpcException ex)
        {
            // The client-side failure channel: the server's `throw new RpcException`
            // (Day-19) crosses the wire and surfaces here as the SAME typed exception.
            // Map the StatusCode to a user-friendly message rather than leaking raw detail.
            ErrorMessage = ex.StatusCode switch
            {
                StatusCode.Unavailable => "Product service is currently unavailable.",
                _ => $"Error: {ex.Status.Detail}"
            };
        }
    }
}
