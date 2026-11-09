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

    private List<(string CustomerName, List<(string ProductName, int Quantity, double UnitPrice)> Items)> NewOrders =
    [
        ("Dave Wilson", [("C# in Depth", 1, 49.99), ("USB-C Hub", 2, 29.99)]),
        ("Eve Brown", [("NAIT Hoodie", 1, 45.00)]),
        ("Frank Lee", [("Wireless Mouse", 3, 24.99), ("Notebook Pack", 5, 12.50)])
    ];

    private async Task BatchCreateAsync()
    {
        try
        {
            IsSending = true;
            StatusMessage = null;
            ErrorMessage = null;

            // Client streaming: open the upstream — no arg, the data goes up RequestStream.
            using var call = OrderClient.BatchCreateOrders();

            foreach (var order in NewOrders)
            {
                var request = new CreateOrderRequest
                {
                    CustomerName = order.CustomerName
                };
                request.Items.AddRange(order.Items.Select(i => new OrderItem
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }));

                // One write per item over the SAME connection — no per-item handshake.
                await call.RequestStream.WriteAsync(request);
            }

            // CompleteAsync is mandatory — the end-of-stream marker that lets the server's
            // `await foreach` finish; without it `await call` never returns. Many writes
            // up, ONE response back, and only after Complete.
            await call.RequestStream.CompleteAsync();
            // The single response arrives after CompleteAsync
            var response = await call;
            StatusMessage = $"Created {response.OrdersCreated} orders. " +
                            $"Total: {response.TotalAmount:C}";
        }
        catch (RpcException ex)
        {
            ErrorMessage = $"Batch create error: {ex.Status.Detail}";
        }
        finally
        {
            IsSending = false;
        }
    }
}
