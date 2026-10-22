using Grpc.Core;
using CampusStore.Protos;

namespace Day20Demo.Server.Services;

public class ProductCatalogService : ProductCatalog.ProductCatalogBase
{
    // In-memory data (a real app would use EF Core)
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
        // Log the authorization header to verify bearer token arrives
      var authHeader = context.RequestHeaders
          .FirstOrDefault(h => h.Key == "authorization")?.Value;
      if (authHeader is not null)
      {
          Console.WriteLine($"Auth header received: {authHeader}");
      }

      var product = Products.FirstOrDefault(p => p.Id == request.Id);

        if (product is null)
        {
            throw new RpcException(
                new Status(StatusCode.NotFound,
                    $"Product {request.Id} not found"));
        }

        return Task.FromResult(new GetProductResponse { Product = product });
    }

    public override Task<ListProductsResponse> ListProducts(
        ListProductsRequest request, ServerCallContext context)
    {
        // Log the authorization header to verify bearer token arrives
      var authHeader = context.RequestHeaders
          .FirstOrDefault(h => h.Key == "authorization")?.Value;
      if (authHeader is not null)
      {
          Console.WriteLine($"Auth header received: {authHeader}");
      }

      var filtered = Products.AsEnumerable();

        if (request.Category != ProductCategory.Unspecified)
        {
            filtered = filtered.Where(p => p.Category == request.Category);
        }

        var response = new ListProductsResponse();
        response.Products.AddRange(filtered);
        return Task.FromResult(response);
    }
}
