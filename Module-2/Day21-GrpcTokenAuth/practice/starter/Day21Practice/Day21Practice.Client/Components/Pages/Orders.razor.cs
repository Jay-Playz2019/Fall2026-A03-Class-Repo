using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day21Practice.Client.Components.Pages;

public partial class Orders : ComponentBase
{
    [Inject]
    private OrderManagement.OrderManagementClient OrderClient { get; set; } = default!;

    private List<Order> OrderList { get; set; } = [];
    private string? ErrorMessage { get; set; }
    private string? CallResult { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await OrderClient.ListOrdersAsync(
                new ListOrdersRequest());
            OrderList = [.. response.Orders];
        }
        catch (RpcException ex)
        {
            ErrorMessage = Describe(ex);
        }
    }

    // PRE-BUILT test calls for Practices 2 and 3 (do not edit).
    private async Task GetOrderOneAsync()
    {
        try
        {
            var response = await OrderClient.GetOrderAsync(new GetOrderRequest { OrderId = 1 });
            CallResult = $"GetOrder succeeded: order {response.Order.OrderId} for {response.Order.CustomerName}.";
        }
        catch (RpcException ex)
        {
            CallResult = $"GetOrder failed ({ex.StatusCode}): {Describe(ex)}";
        }
    }

    private async Task CreateTestOrderAsync()
    {
        try
        {
            var request = new CreateOrderRequest { CustomerName = "Test Customer" };
            request.Items.Add(new OrderItem { ProductId = 1, ProductName = "Test Item", Quantity = 1, UnitPrice = 10.00 });
            var response = await OrderClient.CreateOrderAsync(request);
            CallResult = $"CreateOrder succeeded: created order {response.Order.OrderId}.";
        }
        catch (RpcException ex)
        {
            CallResult = $"CreateOrder failed ({ex.StatusCode}): {Describe(ex)}";
        }
    }

    private static string Describe(RpcException ex) => ex.StatusCode switch
    {
        StatusCode.Unavailable => "Order service is currently unavailable.",
        StatusCode.Unauthenticated => "Authentication required. Check your token.",
        StatusCode.PermissionDenied => "You don't have permission for this action.",
        _ => $"Error: {ex.Status.Detail}"
    };
}
