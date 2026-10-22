using Day19Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// Two registrations wire up a gRPC server — that's all Program.cs needs today.
// 1. AddGrpc() adds the gRPC framework (routing, serialization, the HTTP/2 plumbing) to DI.
builder.Services.AddGrpc();

var app = builder.Build();

// 2. MapGrpcService<T>() maps YOUR concrete service onto endpoint routing so incoming HTTP/2
//    calls for the ProductCatalog contract reach it. The generic argument is your implementation
//    class — not the generated base.
app.MapGrpcService<ProductCatalogService>();

app.Run();
