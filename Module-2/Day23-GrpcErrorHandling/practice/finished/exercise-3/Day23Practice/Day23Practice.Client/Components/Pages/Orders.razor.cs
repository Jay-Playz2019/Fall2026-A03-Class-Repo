using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day23Practice.Client.Components.Pages;

public partial class Orders : ComponentBase
{
    [Inject]
    private OrderManagement.OrderManagementClient OrderClient { get; set; } = default!;

    private List<Order> OrderList { get; set; } = [];
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var response = await OrderClient.ListOrdersAsync(
                new ListOrdersRequest());
            OrderList = [.. response.Orders];
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
