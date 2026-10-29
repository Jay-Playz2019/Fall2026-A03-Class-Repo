using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day22Practice.Client.Components.Pages;

public partial class OrderStream : ComponentBase
{
    [Inject]
    private OrderManagement.OrderManagementClient OrderClient { get; set; } = default!;

    private List<Order> OrderList { get; set; } = [];
    private bool IsStreaming { get; set; }
    private string? ErrorMessage { get; set; }

    private async Task LoadStreamAsync()
    {
        try
        {
            IsStreaming = true;
            OrderList.Clear();
            StateHasChanged();

            // StreamOrders hands back a streaming call, not the data; `using` disposes
            // (cancels) it when this METHOD finishes -- not when we navigate away.
            // ResponseStream arrives item-by-item.
            using var call = OrderClient.StreamOrders(
                new StreamOrdersRequest());

            // ReadAllAsync pulls each order lazily as the server writes it (not buffered).
            await foreach (var order in call.ResponseStream.ReadAllAsync())
            {
                OrderList.Add(order);
                // Gotcha: with no re-render call here, every row appears only at the end.
                // InvokeAsync works from any thread — a bare StateHasChanged works in this
                // click handler, but throws off the UI context. One arriving item = one
                // re-render (the live-feed effect).
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (RpcException ex)
        {
            ErrorMessage = $"Streaming error: {ex.Status.Detail}";
        }
        finally
        {
            IsStreaming = false;
        }
    }
}
