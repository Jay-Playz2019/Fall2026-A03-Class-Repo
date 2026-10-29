using Grpc.Core;
using CampusStore.Protos;
using Microsoft.AspNetCore.Authorization;

namespace Day22Practice.Server.Services;

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
        },
        new()
        {
            OrderId = 3,
            CustomerName = "Carol Davis",
            OrderDate = "2026-02-22",
            TotalAmount = 54.98,
            Status = OrderStatus.Processing,
            Items =
            {
                new OrderItem { ProductId = 4, ProductName = "Wireless Mouse",
                                Quantity = 1, UnitPrice = 24.99 },
                new OrderItem { ProductId = 2, ProductName = "USB-C Hub",
                                Quantity = 1, UnitPrice = 29.99 }
            }
        }
    ];

    public override Task<GetOrderResponse> GetOrder(
        GetOrderRequest request, ServerCallContext context)
    {
        var order = Orders.FirstOrDefault(o => o.OrderId == request.OrderId);
        if (order is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Order {request.OrderId} not found"));
        return Task.FromResult(new GetOrderResponse { Order = order });
    }

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

    // Practice 1: Server streaming — one request, MANY responses out the
    // IServerStreamWriter pipe; WriteAsync once per item, and returning closes the stream.
    public override async Task StreamOrders(
        StreamOrdersRequest request,
        // The outbound pipe: write each item to the client
        IServerStreamWriter<Order> responseStream,
        ServerCallContext context)
    {
        // Snapshot before streaming: a concurrent BatchCreateOrders call mutates the
        // same static Orders list and would invalidate this enumerator mid-stream
        // ("Collection was modified; enumeration operation may not execute"). ToList
        // freezes the view for this call. (Same guard as the StreamProducts demo.)
        var filtered = Orders.AsEnumerable();
        if (request.StatusFilter != OrderStatus.Unspecified)
            filtered = filtered.Where(o => o.Status == request.StatusFilter);
        // Freeze the list before streaming
        var snapshot = filtered.ToList();

        foreach (var order in snapshot)
        {
            // Stop if the client went away
            if (context.CancellationToken.IsCancellationRequested)
                break;

            // Send one item now
            await responseStream.WriteAsync(order);
            // Simulate a delay so the feed visibly grows
            await Task.Delay(500, context.CancellationToken);
        }
    }

    // Practice 2: Client streaming — MANY requests in via the IAsyncStreamReader, drained
    // with `await foreach`; the client's CompleteAsync ends the loop, then we return the
    // single aggregate response.
    public override async Task<BatchCreateOrdersResponse> BatchCreateOrders(
        // The inbound pipe: read what the client sends
        IAsyncStreamReader<CreateOrderRequest> requestStream,
        ServerCallContext context)
    {
        var count = 0;
        var totalAmount = 0.0;

        // Read each item as the client sends it
        await foreach (var request in requestStream.ReadAllAsync(
            context.CancellationToken))
        {
            // Resilient batch: skip an invalid row rather than aborting the whole
            // stream (unlike the unary CreateOrder, which throws). We log the skip so
            // it is not silent. Same empty-items guard the unary path enforces.
            if (string.IsNullOrWhiteSpace(request.CustomerName) || request.Items.Count == 0)
            {
                Console.WriteLine($"Skipped invalid order row (name='{request.CustomerName}', items={request.Items.Count})");
                continue;
            }

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

            count++;
            totalAmount += order.TotalAmount;
            // Log each item as it arrives
            Console.WriteLine($"Received order for: {request.CustomerName}");
        }

        // One response, after the client completes
        return new BatchCreateOrdersResponse
        {
            OrdersCreated = count,
            TotalAmount = totalAmount
        };
    }
}
