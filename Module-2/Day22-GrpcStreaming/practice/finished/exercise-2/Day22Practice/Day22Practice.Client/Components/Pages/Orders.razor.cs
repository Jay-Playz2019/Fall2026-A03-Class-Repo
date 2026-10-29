using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day22Practice.Client.Components.Pages;

public partial class Orders : ComponentBase
{
    [Inject]
    private OrderManagement.OrderManagementClient OrderClient { get; set; } = default!;

    private List<Order> OrderList { get; set; } = [];
    private string? ErrorMessage { get; set; }

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
            ErrorMessage = ex.StatusCode switch
            {
                StatusCode.Unavailable => "Order service is currently unavailable.",
                StatusCode.Unauthenticated => "Authentication required. Check your token.",
                StatusCode.PermissionDenied => "You don't have permission for this action.",
                _ => $"Error: {ex.Status.Detail}"
            };
        }
    }
}
