using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day23Practice.Client.Components.Pages;

public partial class OrderDetail : ComponentBase
{
    [Inject]
    private OrderManagement.OrderManagementClient OrderClient { get; set; } = default!;

    private int OrderId { get; set; } = 1;
    private Order? SelectedOrder { get; set; }
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    private async Task LookupOrder()
    {
        ErrorMessage = null;
        SelectedOrder = null;
        IsLoading = true;

        try
        {
            var response = await OrderClient.GetOrderAsync(
                new GetOrderRequest { OrderId = OrderId });
            SelectedOrder = response.Order;
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
