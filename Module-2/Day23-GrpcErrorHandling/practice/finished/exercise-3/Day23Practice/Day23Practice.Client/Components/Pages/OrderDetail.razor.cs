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
            // Practice 2: deadline = a fail-fast budget for the whole call, sent as an
            // absolute UTC instant. Without it a hung server freezes this UI forever;
            // with it the call aborts at 3s as DeadlineExceeded.
            var deadline = DateTime.UtcNow.AddSeconds(3);
            var response = await OrderClient.GetOrderAsync(
                new GetOrderRequest { OrderId = OrderId },
                // Abort the call at the deadline
                deadline: deadline);
            SelectedOrder = response.Order;
        }
        // Exception filters: `when (ex.StatusCode == …)` runs only the matching arm, so
        // each gRPC status maps to its own user message.
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            // The 3-second deadline fired
            ErrorMessage = "Request timed out. Please try again.";
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            // The server's NotFound; Detail is its message
            ErrorMessage = $"Order not found: {ex.Status.Detail}";
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            // The server is down or unreachable
            ErrorMessage = "Order service is currently unavailable.";
        }
        // Catch-all LAST — after the typed filters, or it would swallow them.
        catch (RpcException ex)
        {
            // Any other status, with its code and detail
            ErrorMessage = $"Service error ({ex.StatusCode}): {ex.Status.Detail}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
