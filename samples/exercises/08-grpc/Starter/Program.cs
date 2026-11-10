using GrpcOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<IOrderTracker, SimulatedOrderTracker>();

// TODO 1: register gRPC's services with builder.Services.AddGrpc().

var app = builder.Build();

// TODO 1 (continued): map the orders service, so its RPCs have endpoints:
// app.MapGrpcService<OrdersService>().

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
