using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day22Practice.Client.Components.Pages;

public partial class BatchOrders : ComponentBase
{
    [Inject]
    private OrderManagement.OrderManagementClient OrderClient { get; set; } = default!;

    private bool IsSending { get; set; }
    private string? StatusMessage { get; set; }
    private string? ErrorMessage { get; set; }

    // Pre-built sample data for exercises
    private List<(string CustomerName, List<(string ProductName, int Quantity, double UnitPrice)> Items)> NewOrders =
    [
        ("Dave Wilson", [("C# in Depth", 1, 49.99), ("USB-C Hub", 2, 29.99)]),
        ("Eve Brown", [("NAIT Hoodie", 1, 45.00)]),
        ("Frank Lee", [("Wireless Mouse", 3, 24.99), ("Notebook Pack", 5, 12.50)])
    ];

    private async Task BatchCreateAsync()
    {
        await Task.CompletedTask;
    }
}
