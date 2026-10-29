using Grpc.Core;
using CampusStore.Protos;
using Microsoft.AspNetCore.Authorization;

namespace Day22Demo.Server.Services;

[Authorize]
public class ProductCatalogService : ProductCatalog.ProductCatalogBase
{
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
        var product = Products.FirstOrDefault(p => p.Id == request.Id);
        if (product is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Product {request.Id} not found"));
        return Task.FromResult(new GetProductResponse { Product = product });
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

    // Server streaming RPC — one request in, MANY responses out over a single open
    // HTTP/2 stream. The shape differs from a unary RPC: there's no Task<TResponse> to
    // return. Instead the framework hands you an IServerStreamWriter<Product> (the
    // outbound pipe) and you WriteAsync into it once per item. There is no CompleteAsync
    // and nothing explicit to "finish" — when this method returns, the framework closes
    // the stream, and that close IS the client's end-of-stream signal.
    public override async Task StreamProducts(
        StreamProductsRequest request,
        IServerStreamWriter<Product> responseStream,
        ServerCallContext context)
    {
        // Snapshot the list before we start streaming. A concurrent BatchAddProducts
        // call on the same in-memory list would invalidate the enumerator mid-loop
        // ("Collection was modified; enumeration operation may not execute"). ToList
        // freezes the view for this stream without blocking writes on the other call.
        var filtered = Products.AsEnumerable();
        if (request.Category != ProductCategory.Unspecified)
            filtered = filtered.Where(p => p.Category == request.Category);
        var snapshot = filtered.ToList();

        foreach (var product in snapshot)
        {
            // Honor cancellation: if the client navigates away or disposes the call, the
            // token trips and we stop, instead of writing into a stream nobody is reading.
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await responseStream.WriteAsync(product);          // push one item across the wire
            await Task.Delay(500, context.CancellationToken);  // simulate live arrival so the feed visibly grows
        }
    }

    // Client streaming RPC — the mirror image of StreamProducts: MANY requests in, ONE
    // response out. The parameter is an IAsyncStreamReader<AddProductRequest> (the
    // inbound pipe), and we drain it with `await foreach ... ReadAllAsync()` — each
    // iteration is one item the client uploaded over the SAME connection (no per-item
    // handshake, the win over N unary calls). The loop ends when the client calls
    // CompleteAsync(); only then do we build and return the single aggregate response.
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
}
