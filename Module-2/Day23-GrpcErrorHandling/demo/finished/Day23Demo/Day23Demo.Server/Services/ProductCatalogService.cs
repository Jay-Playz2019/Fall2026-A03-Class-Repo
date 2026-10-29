using Grpc.Core;
using CampusStore.Protos;
using Microsoft.AspNetCore.Authorization;
using BYSResults;
using Day23Demo.Server.Extensions;

namespace Day23Demo.Server.Services;

[Authorize]
public class ProductCatalogService : ProductCatalog.ProductCatalogBase
{
    // StockQuantity (proto field 5) is intentionally left at its proto3
    // default of 0 — stock levels are not modelled in this error-handling demo.
    private static readonly List<Product> Products =
    [
        new() { Id = 1, Name = "C# in Depth", Price = 49.99,
                Category = ProductCategory.Books },
        new() { Id = 2, Name = "USB-C Hub", Price = 29.99,
                Category = ProductCategory.Electronics },
        new() { Id = 3, Name = "NAIT Hoodie", Price = 45.00,
                Category = ProductCategory.Clothing },
        new() { Id = 4, Name = "Wireless Mouse", Price = 24.99,
                Category = ProductCategory.Electronics },
        new() { Id = 5, Name = "Notebook Pack", Price = 12.50,
                Category = ProductCategory.Supplies }
    ];

    public override Task<GetProductResponse> GetProduct(
        GetProductRequest request, ServerCallContext context)
    {
        // The day's pattern in three lines: do the work as a Result, and on failure
        // THROW the mapped RpcException. Days 19-22 hand-built `throw new RpcException(
        // new Status(StatusCode.NotFound, …))` at each miss; today the lookup returns a
        // Result and ToRpcException() does the status mapping, so every method reports
        // failures the same way. Note we throw rather than return — that's how a unary
        // gRPC method signals failure (a Task<TResponse> has no "failed" return shape).
        var result = FindProduct(request.Id);
        // Failure: the extension picks the status code
        if (!result.IsSuccess)
            throw result.ToRpcException();

        // Success: unwrap the product
        return Task.FromResult(new GetProductResponse { Product = result.Value! });
    }

    [Authorize(Policy = "AdminOnly")]
    public override Task<ListProductsResponse> ListProducts(
        ListProductsRequest request, ServerCallContext context)
    {
        var filtered = Products.AsEnumerable();
        if (request.Category != ProductCategory.Unspecified)
            filtered = filtered.Where(p => p.Category == request.Category);
        var response = new ListProductsResponse();
        response.Products.AddRange(filtered);
        return Task.FromResult(response);
    }

    public override async Task StreamProducts(
        StreamProductsRequest request,
        IServerStreamWriter<Product> responseStream,
        ServerCallContext context)
    {
        var filtered = Products.AsEnumerable();
        if (request.Category != ProductCategory.Unspecified)
            filtered = filtered.Where(p => p.Category == request.Category);

        foreach (var product in filtered)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await responseStream.WriteAsync(product);
            await Task.Delay(500, context.CancellationToken);
        }
    }

    public override async Task<BatchAddProductsResponse> BatchAddProducts(
        IAsyncStreamReader<AddProductRequest> requestStream,
        ServerCallContext context)
    {
        var count = 0;
        await foreach (var request in requestStream.ReadAllAsync(
            context.CancellationToken))
        {
            Products.Add(new Product
            {
                Id = Products.Count + 1,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category
            });
            count++;
            Console.WriteLine($"Received product: {request.Name}");
        }
        return new BatchAddProductsResponse { ProductsAdded = count };
    }

    // The Result-producing half: the lookup decides success/failure in domain terms
    // and stays completely gRPC-unaware — no RpcException, no StatusCode here. The
    // Error CODE "NotFound" is the contract key BYSResultExtensions maps to
    // StatusCode.NotFound. Static factories (Success/Failure) are the package's
    // shorter one-outcome shape; Day 6 used the instance API (new Result<T>() +
    // .AddError(...)) — same package, same Result<T>, just the terser helper here.
    private static Result<Product> FindProduct(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        // Found: Success; missing: a NotFound Error
        return product is not null
            ? Result<Product>.Success(product)
            : Result<Product>.Failure(new Error("NotFound", $"Product {id} not found"));
    }
}
