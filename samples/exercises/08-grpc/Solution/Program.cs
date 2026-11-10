using GrpcOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<IOrderTracker, SimulatedOrderTracker>();

// gRPC's services, then one endpoint per service. Each call to an RPC creates an OrdersService,
// like a controller per request, so it can take scoped services too.
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<OrdersService>();

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
