using Grpc.Core;
using CampusStore.Protos;

namespace Day19Practice.Services;

// Inherit the base the build generated from the OrderManagement service block in order.proto,
// then override each RPC. You extend the generated base — you never edit it.
public class OrderManagementService : OrderManagement.OrderManagementBase
{
    // static so the data survives across calls — gRPC builds a fresh service instance per request.
    private static readonly List<Order> Orders =
    [
        new()
        {
            OrderId = 1,
            CustomerName = "Alice Johnson",
            OrderDate = "2026-02-15",
            TotalAmount = 79.98,
            Status = OrderStatus.Delivered,
            Items =
            {
                new OrderItem { ProductId = 1, ProductName = "C# in Depth",
                                Quantity = 1, UnitPrice = 49.99 },
                new OrderItem { ProductId = 2, ProductName = "USB-C Hub",
                                Quantity = 1, UnitPrice = 29.99 }
            }
        },
        new()
        {
            OrderId = 2,
            CustomerName = "Bob Smith",
            OrderDate = "2026-02-20",
            TotalAmount = 45.00,
            Status = OrderStatus.Pending,
            Items =
            {
                new OrderItem { ProductId = 3, ProductName = "NAIT Hoodie",
                                Quantity = 1, UnitPrice = 45.00 }
            }
        }
    ];

    public override Task<GetOrderResponse> GetOrder(
        GetOrderRequest request, ServerCallContext context)
    {
        var order = Orders.FirstOrDefault(o => o.OrderId == request.OrderId);

        // Signal failure by throwing RpcException + StatusCode — never by returning null.
        if (order is null)
        {
            throw new RpcException(
                new Status(StatusCode.NotFound,
                    $"Order {request.OrderId} not found"));
        }

        return Task.FromResult(new GetOrderResponse { Order = order });  // sync work wrapped — the override returns a Task
    }

    public override Task<ListOrdersResponse> ListOrders(
        ListOrdersRequest request, ServerCallContext context)
    {
        var filtered = Orders.AsEnumerable();

        // Unspecified = the proto3 enum-0 default = client sent no filter.
        if (request.StatusFilter != OrderStatus.Unspecified)
        {
            filtered = filtered.Where(o => o.Status == request.StatusFilter);
        }

        var response = new ListOrdersResponse();
        response.Orders.AddRange(filtered);  // 'repeated' field is read-only — AddRange, don't assign
        return Task.FromResult(response);
    }
}
