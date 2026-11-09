using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day23Demo.Client.Components.Pages;

public partial class ProductLookup : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private int ProductId { get; set; } = 1;
    private Product? SelectedProduct { get; set; }
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    private async Task LookupProduct()
    {
        ErrorMessage = null;
        SelectedProduct = null;
        IsLoading = true;

        try
        {
            // DEADLINE = fail-fast budget for the WHOLE call, sent to the server as an
            // absolute UTC instant (client + server agree on one wall-clock cutoff rather
            // than each timing a duration). Without it, a hung/unresponsive server freezes
            // this UI indefinitely; with it the call aborts at 3s as DeadlineExceeded.
            var deadline = DateTime.UtcNow.AddSeconds(3);
            var response = await ProductClient.GetProductAsync(
                new GetProductRequest { Id = ProductId },
                // Abort the call at the deadline
                deadline: deadline);
            SelectedProduct = response.Product;
        }
        // Client mirror of the server's mapping: each gRPC StatusCode → a specific,
        // user-facing message. `when (ex.StatusCode == …)` is an EXCEPTION FILTER —
        // only the matching arm runs; non-matches keep unwinding. Cleaner than one
        // catch with an inner if/switch.
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            // The deadline above fired — the call exceeded its time budget.
            ErrorMessage = "Request timed out. Please try again.";
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            // Server's Error("NotFound", …) arrives here; Detail is the message it set.
            ErrorMessage = $"Product not found: {ex.Status.Detail}";
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            // Server unreachable. The retry policy already tried 3× before this surfaced.
            ErrorMessage = "Service is currently unavailable. Please try again later.";
        }
        catch (RpcException ex)
        {
            // Catch-all for any unmatched status (e.g. Internal). Must come LAST, after
            // the typed filters, or it would swallow the specific cases above.
            ErrorMessage = $"Service error ({ex.StatusCode}): {ex.Status.Detail}";
        }
        finally
        {
            // finally, not per-arm: the spinner clears whether the call succeeded or threw.
            IsLoading = false;
        }
    }
}
