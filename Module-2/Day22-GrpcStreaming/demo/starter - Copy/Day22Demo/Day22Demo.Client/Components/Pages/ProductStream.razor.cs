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
        try
        {
            // clear the list of Products so we get a fresh one. Trigger the state has changed so that the table refreshes.
            IsStreaming = true;
            ProductList.Clear();
            StateHasChanged();

            // Start the stream; disposing the call cancels it
            // StreamProducts returns a Call, not the data. The `using` part here is critical. When the call is disposed, the stream stops.
            // placing 'using' here scopes this variable to the method, so the stream closes once the method finishes, rather than we when the component unmounts.
            // we don't need to unmount a component to close the stream.
            using var call = ProductClient.StreamProducts(
                new StreamProductsRequest());

            // To access the stream we use call.ResponseStream. To read the items as they come in, we have the following line:
            // ReadAllAsync() is not bufferred, the table grows live instead of waiting for everything
            await foreach (var product in call.ResponseStream.ReadAllAsync())
            {
                ProductList.Add(product);
                // Re-render once per arriving item. Without this call, the table would only update once everything has returned. 
                // This gives us the 'live-feed' effect of having a re-render after each item loaded.
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (RpcException ex)
        {
            // A server-side fault (or a dropped stream) surfaces here as the SAME
            // RpcException type, carrying the server's Status.Detail across the wire.
            ErrorMessage = $"Streaming error: {ex.Status.Detail}";
        }
        finally
        {
            IsStreaming = false;
        }
    }
}
