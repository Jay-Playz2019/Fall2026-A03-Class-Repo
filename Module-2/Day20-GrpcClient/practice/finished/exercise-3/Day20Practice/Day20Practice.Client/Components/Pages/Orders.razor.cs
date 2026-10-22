using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day20Practice.Client.Components.Pages;

public partial class Orders : ComponentBase
{
    // Generated from order.proto, injected via AddGrpcClient<T>() — no URL, the
    // contract is the type.
    [Inject]
    private OrderManagement.OrderManagementClient OrderClient { get; set; } = default!;

    // Practice 1: Order list
    private List<Order> OrderList { get; set; } = [];
    private string? ErrorMessage { get; set; }

    // Practice 2: Search by ID
    private int SearchId { get; set; }
    private Order? FoundOrder { get; set; }
    private string? SearchError { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Strongly-typed RPC, not a string URL; Orders is the proto's repeated field.
            var response = await OrderClient.ListOrdersAsync(
                new ListOrdersRequest());
            OrderList = [.. response.Orders];
        }
        catch (RpcException ex)
        {
            // The server's RpcException crosses the wire and lands here as the same type.
            ErrorMessage = $"Error loading orders: {ex.Status.Detail}";
        }
    }

    // Practice 2: Search with error handling
    private async Task SearchOrder()
    {
        FoundOrder = null;
        SearchError = null;

        try
        {
            // A unary RPC fired by a user action (the Search button), not a lifecycle
            // method — same typed-call shape as ListOrders, just on demand.
            var response = await OrderClient.GetOrderAsync(
                new GetOrderRequest { OrderId = SearchId });
            FoundOrder = response.Order;
        }
        catch (RpcException ex)
        {
            // Beyond Practice 1's bare Detail: map each StatusCode the client may see
            // to a message the user can act on. GetOrder itself returns only NotFound;
            // the InvalidArgument arm is defensive (only CreateOrder throws it).
            SearchError = ex.StatusCode switch
            {
                StatusCode.NotFound => "Order not found. Check the ID and try again.",
                StatusCode.InvalidArgument => "Invalid order ID.",
                StatusCode.Unavailable => "Service temporarily unavailable.",
                _ => $"Error: {ex.Status.Detail}"
            };
        }
    }
}
