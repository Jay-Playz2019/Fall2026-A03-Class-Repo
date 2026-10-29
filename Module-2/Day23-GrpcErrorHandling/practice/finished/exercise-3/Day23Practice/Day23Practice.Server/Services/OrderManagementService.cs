using Grpc.Core;
using CampusStore.Protos;
using Microsoft.AspNetCore.Authorization;
using BYSResults;
using Day23Practice.Server.Extensions;

namespace Day23Practice.Server.Services;

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
        // Practice 1: do the work as a Result, then THROW the mapped RpcException on
        // failure. ToRpcException() turns the domain Error code into a gRPC StatusCode,
        // so every method reports failures the same way instead of hand-built throws.
        var result = FindOrder(request.OrderId);
        // Failure: the extension picks the status code
        if (!result.IsSuccess)
            throw result.ToRpcException();

        // Success: unwrap the order
        return Task.FromResult(new GetOrderResponse { Order = result.Value! });
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
        // Practice 4 (stretch): same Result→throw shape as GetOrder, but the failure is a
        // "Validation" Error → InvalidArgument (not NotFound). Validate FIRST, before
        // touching state, so a bad request never half-creates an order.
        var validationResult = ValidateCreateOrder(request);
        // Invalid: throw before touching any state
        if (!validationResult.IsSuccess)
            throw validationResult.ToRpcException();

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

    public override async Task StreamOrders(
        StreamOrdersRequest request,
        IServerStreamWriter<Order> responseStream,
        ServerCallContext context)
    {
        var filtered = Orders.AsEnumerable();
        if (request.StatusFilter != OrderStatus.Unspecified)
            filtered = filtered.Where(o => o.Status == request.StatusFilter);

        foreach (var order in filtered)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await responseStream.WriteAsync(order);
            await Task.Delay(500, context.CancellationToken);
        }
    }

    public override async Task<BatchCreateOrdersResponse> BatchCreateOrders(
        IAsyncStreamReader<CreateOrderRequest> requestStream,
        ServerCallContext context)
    {
        var count = 0;
        var totalAmount = 0.0;

        await foreach (var request in requestStream.ReadAllAsync(
            context.CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                continue;

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
            Console.WriteLine($"Received order for: {request.CustomerName}");
        }

        return new BatchCreateOrdersResponse
        {
            OrdersCreated = count,
            TotalAmount = totalAmount
        };
    }

    // gRPC-unaware: decides success/failure in domain terms only. The Error code
    // "NotFound" is the contract key BYSResultExtensions maps to StatusCode.NotFound.
    private static Result<Order> FindOrder(int orderId)
    {
        var order = Orders.FirstOrDefault(o => o.OrderId == orderId);
        // Found: Success; missing: a NotFound Error
        return order is not null
            ? Result<Order>.Success(order)
            : Result<Order>.Failure(new Error("NotFound", $"Order {orderId} not found"));
    }

    // Result<bool> = a yes/no outcome (the bool payload is unused on success — the point
    // is the pass/fail). Each "Validation"-coded Error maps to InvalidArgument client-side.
    private static Result<bool> ValidateCreateOrder(CreateOrderRequest request)
    {
        // No customer name: a Validation Error
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return Result<bool>.Failure(new Error("Validation", "Customer name is required"));
        // No items: a Validation Error
        if (request.Items.Count == 0)
            return Result<bool>.Failure(new Error("Validation", "Order must have at least one item"));
        // Every check passed
        return Result<bool>.Success(true);
    }
}
