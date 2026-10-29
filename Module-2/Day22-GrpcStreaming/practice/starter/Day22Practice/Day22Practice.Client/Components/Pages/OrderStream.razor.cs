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
        await Task.CompletedTask;
    }
}
