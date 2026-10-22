using Grpc.Core;
using CampusStore.Protos;   // generated from product.proto at build time — the message classes live here

namespace Day19Demo.Services;

// The service implementation. Inherit the abstract base the BUILD generated from the .proto
// (the `service ProductCatalog` block → ProductCatalog.ProductCatalogBase) and override each RPC.
// This is the "inherit and override" half of the day: you never touch the generated base in obj/,
// you extend it. Every RPC the .proto declared appears as a virtual method that throws
// Unimplemented until you override it here.
public class ProductCatalogService : ProductCatalog.ProductCatalogBase
{
    // In-memory data (a real app would use EF Core). static so the data survives across calls —
    // gRPC creates a fresh service instance per request, so per-instance fields would reset.
    private static readonly List<Product> Products =
    [
        new() { Id = 1, Name = "C# in Depth", Price = 49.99,
                Category = ProductCategory.Books },
        new() { Id = 2, Name = "USB-C Hub", Price = 29.99,
                Category = ProductCategory.Electronics },
        new() { Id = 3, Name = "NAIT Hoodie", Price = 45.00,
                Category = ProductCategory.Clothing }
    ];

    // A unary RPC: one request in, one response out. The generated signature is always
    // Task<TResponse> Method(TRequest, ServerCallContext) — async-shaped even when the work is
    // synchronous (hence Task.FromResult below). ServerCallContext carries per-call metadata
    // (deadline, headers, cancellation) — unused here but always present.
    public override Task<GetProductResponse> GetProduct(
        GetProductRequest request, ServerCallContext context)
    {
        var product = Products.FirstOrDefault(p => p.Id == request.Id);

        // gRPC signals failure by THROWING an RpcException carrying a StatusCode — not by returning
        // null. The status (NotFound) is the contract's error vocabulary; the client receives it as
        // a typed status, not an HTTP 500. Returning an empty Product would read as success.
        if (product is null)
        {
            throw new RpcException(
                new Status(StatusCode.NotFound,
                    $"Product {request.Id} not found"));
        }

        // No awaiting needed, so wrap the value — the override demands a Task.
        return Task.FromResult(new GetProductResponse { Product = product });
    }

    public override Task<ListProductsResponse> ListProducts(
        ListProductsRequest request, ServerCallContext context)
    {
        var filtered = Products.AsEnumerable();

        // Unspecified is the proto3 enum-0 default: when the client omits Category it arrives as
        // Unspecified, which we treat as "no filter." This is exactly why proto3 reserves 0 for
        // UNSPECIFIED (Day 17) — without it, "not set" and a real value would be indistinguishable.
        if (request.Category != ProductCategory.Unspecified)
        {
            filtered = filtered.Where(p => p.Category == request.Category);
        }

        // A 'repeated' field generates a read-only RepeatedField<T> — you can't assign it, so build
        // the response and AddRange into its collection.
        var response = new ListProductsResponse();
        response.Products.AddRange(filtered);
        return Task.FromResult(response);
    }
}
