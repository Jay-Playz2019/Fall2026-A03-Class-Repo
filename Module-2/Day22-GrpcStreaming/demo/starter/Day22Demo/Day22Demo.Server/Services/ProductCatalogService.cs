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
}
