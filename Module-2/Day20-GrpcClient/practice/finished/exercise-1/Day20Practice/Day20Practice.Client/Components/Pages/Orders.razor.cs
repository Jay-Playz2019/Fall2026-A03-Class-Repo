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
}
