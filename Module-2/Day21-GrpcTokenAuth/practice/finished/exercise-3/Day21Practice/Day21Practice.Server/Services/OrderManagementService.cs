using Grpc.Core;
using CampusStore.Protos;
using Microsoft.AspNetCore.Authorization;

namespace Day21Practice.Server.Services;

// Practice 1: class-level [Authorize] gates every RPC — a valid bearer token is
// now required to call any method, rejected before the body runs.
[Authorize]
public class OrderManagementService : OrderManagement.OrderManagementBase
{
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

    // Practice 3, step 4: read the caller's identity inside the RPC. GetHttpContext().User
    // bridges gRPC's ServerCallContext to the ASP.NET ClaimsPrincipal built from the
    // validated token; FindFirst("name") reads one claim (short key kept by MapInboundClaims=false).
    public override Task<GetOrderResponse> GetOrder(
        GetOrderRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var callerName = httpContext.User.FindFirst("name")?.Value ?? "Unknown";
        Console.WriteLine($"GetOrder called by: {callerName}");

        var order = Orders.FirstOrDefault(o => o.OrderId == request.OrderId);
        if (order is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Order {request.OrderId} not found"));
        return Task.FromResult(new GetOrderResponse { Order = order });
    }

    // Practice 2: [AllowAnonymous] opts THIS one method back out of the class-level
    // gate — a public read path that callers can reach without a token.
    [AllowAnonymous]
    public override Task<ListOrdersResponse> ListOrders(
        ListOrdersRequest request, ServerCallContext context)
    {
        var filtered = Orders.AsEnumerable();
        if (request.StatusFilter != OrderStatus.Unspecified)
            filtered = filtered.Where(o => o.Status == request.StatusFilter);
        var response = new ListOrdersResponse();
        response.Orders.AddRange(filtered);
        return Task.FromResult(response);
    }

    // Practice 3, steps 1-3: tighter than the class gate — [Authorize(Policy = "AdminOnly")]
    // also needs a role=admin claim (the policy defined in Program.cs), so a valid
    // non-admin token authenticates but is still refused here.
    [Authorize(Policy = "AdminOnly")]
    public override Task<CreateOrderResponse> CreateOrder(
        CreateOrderRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Customer name is required"));
        if (request.Items.Count == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Order must have at least one item"));

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
