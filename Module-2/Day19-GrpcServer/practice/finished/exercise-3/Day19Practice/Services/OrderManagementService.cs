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

    public override Task<CreateOrderResponse> CreateOrder(
        CreateOrderRequest request, ServerCallContext context)
    {
        // Validate on the server and reject bad input with InvalidArgument — a different status from
        // GetOrder's NotFound. NotFound = "the thing you asked for isn't here"; InvalidArgument =
        // "your request itself is malformed." Matching the right StatusCode to the failure is the point.
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument,
                    "Customer name is required"));
        }

        if (request.Items.Count == 0)
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument,
                    "Order must have at least one item"));
        }

        // Compute the total server-side — never trust a client-supplied amount; the server owns the
        // money. OrderId and Status are assigned here too, not taken from the request.
        var order = new Order
        {
            OrderId = Orders.Count + 1,
            CustomerName = request.CustomerName,
            OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Status = OrderStatus.Pending,
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice)
        };
        order.Items.AddRange(request.Items);

        Orders.Add(order);

        return Task.FromResult(new CreateOrderResponse { Order = order });
    }
}
