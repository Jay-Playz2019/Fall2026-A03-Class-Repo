using Grpc.Core;
using CampusStore.Protos;
using Microsoft.AspNetCore.Authorization;

namespace Day21Demo.Server.Services;

// [Authorize] at the class level gates EVERY RPC: a valid bearer token is now
// required to call any method here. The framework returns Unauthenticated before
// the method body runs — the same declarative attribute used on Blazor pages
// (Day-14), applied to a gRPC service.
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
                Category = ProductCategory.Clothing }
    ];

    public override Task<GetProductResponse> GetProduct(
        GetProductRequest request, ServerCallContext context)
    {
        // Today's headline concept: reach the caller's identity from inside an RPC.
        // ServerCallContext is gRPC's abstraction; GetHttpContext().User bridges to
        // the ASP.NET ClaimsPrincipal that AddJwtBearer built from the validated token.
        // FindFirst reads a single claim by name (works because MapInboundClaims = false
        // kept the short "name"/"role" keys). These are the same claims Day-12 covered.
        var httpContext = context.GetHttpContext();
        var userName = httpContext.User.FindFirst("name")?.Value ?? "Unknown";
        var userRole = httpContext.User.FindFirst("role")?.Value ?? "none";
        Console.WriteLine($"GetProduct called by {userName} (role: {userRole})");

        var product = Products.FirstOrDefault(p => p.Id == request.Id);
        if (product is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Product {request.Id} not found"));
        return Task.FromResult(new GetProductResponse { Product = product });
    }

    // Tighter than the class-level [Authorize]: this method also requires the
    // "AdminOnly" policy (a role=admin claim), so a valid NON-admin token passes
    // authentication but is still refused here. Method attributes stack on the class one.
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
}
