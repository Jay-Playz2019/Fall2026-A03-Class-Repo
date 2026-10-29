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
            IsStreaming = true;
            ProductList.Clear();
            StateHasChanged();

            // A server-streaming client call returns the CALL, not the data. `using`
            // matters: disposing the call cancels the server stream -- but the `using` is
            // method-scoped, so that happens when THIS METHOD finishes, not when the
            // component unmounts. The request goes in now;
            // the responses arrive over time on call.ResponseStream.
            using var call = ProductClient.StreamProducts(
                new StreamProductsRequest());

            // Pull items lazily, one at a time, as the server writes them. ReadAllAsync
            // is not buffered — the table grows live instead of waiting for the full set.
            await foreach (var product in call.ResponseStream.ReadAllAsync())
            {
                ProductList.Add(product);
                // THE streaming-in-Blazor gotcha: with no re-render call here, every row
                // appears only when the stream ends. InvokeAsync runs StateHasChanged on
                // the UI context from any thread — a bare call works in this click handler,
                // but throws if the stream is started off the UI (timer, background service).
                // One arriving item = one re-render (the live-feed effect).
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
