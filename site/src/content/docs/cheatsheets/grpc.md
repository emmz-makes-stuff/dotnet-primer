---
title: gRPC cheatsheet
description: A .proto skeleton, code generation, hosting, calls and streams, deadlines, status codes, rich errors and health checks, on one page.
prev: false
next: false
learn:
  - title: gRPC on .NET overview
    url: https://learn.microsoft.com/aspnet/core/grpc/
  - title: Create Protobuf messages for .NET apps
    url: https://learn.microsoft.com/aspnet/core/grpc/protobuf
  - title: Reliable gRPC services with deadlines and cancellation
    url: https://learn.microsoft.com/aspnet/core/grpc/deadlines-cancellation
  - title: Error handling with gRPC
    url: https://learn.microsoft.com/aspnet/core/grpc/error-handling
---

Back to [gRPC](/services/grpc/).

## Contract

```proto title="Protos/orders.proto"
syntax = "proto3";

option csharp_namespace = "Shop.Orders";   // the C# namespace; not on the wire

package orders.v1;                          // on the wire: part of every method's path

import "google/protobuf/timestamp.proto";

service Orders {
  rpc GetOrder (GetOrderRequest) returns (Order);                      // unary
  rpc TrackOrder (TrackOrderRequest) returns (stream OrderStatusUpdate); // server streaming
  rpc ImportOrders (stream PlaceOrderRequest) returns (ImportSummary);   // client streaming
  rpc WatchOrders (stream TrackOrderRequest) returns (stream OrderStatusUpdate); // both ways
}

message Order {
  int32 id = 1;                // the number goes on the wire, not the name
  string customer_email = 2;
  repeated OrderLine lines = 3;
  int64 total_cents = 4;       // no decimal type: minor units, or a units-and-nanos message
  google.protobuf.Timestamp placed_at = 5;
  reserved 6;                  // a removed field: its number and name are never reused
  reserved "status";
}
```

## Code generation

```xml title="Server.csproj"
<ItemGroup>
  <Protobuf Include="Protos/orders.proto" GrpcServices="Server" />
  <PackageReference Include="Grpc.AspNetCore" />          <!-- brings Grpc.Tools and Google.Protobuf -->
</ItemGroup>
```

```xml title="Client.csproj"
<ItemGroup>
  <Protobuf Include="Protos/orders.proto" GrpcServices="Client" />
  <PackageReference Include="Grpc.Net.Client" />
  <PackageReference Include="Google.Protobuf" />
  <PackageReference Include="Grpc.Tools" PrivateAssets="all" />   <!-- build time only -->
</ItemGroup>
```

`GrpcServices="Both"` for an app that serves and calls. The generated code is in `obj/` and is rebuilt on every build.

## Host a service

```csharp title="Program.cs"
builder.Services.AddGrpc(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());
var app = builder.Build();
app.MapGrpcService<OrdersService>();
```

```csharp title="OrdersService.cs"
public sealed class OrdersService(OrderStore store) : Orders.OrdersBase
{
    public override Task<Order> GetOrder(GetOrderRequest request, ServerCallContext context) =>
        store.Find(request.OrderId) is { } order
            ? Task.FromResult(order)
            : throw new RpcException(new Status(StatusCode.NotFound, $"There's no order {request.OrderId}."));
}
```

```json title="appsettings.json"
{
  "Kestrel": {
    "Endpoints": {
      "Grpc": { "Url": "http://localhost:5287", "Protocols": "Http2" }
    }
  }
}
```

Over plain HTTP, the default `Http1AndHttp2` serves HTTP/1.1 only, and every call fails with `HTTP_1_1_REQUIRED`.

## Call a service

```csharp
using var channel = GrpcChannel.ForAddress("http://localhost:5287");   // create once, reuse
var client = new Orders.OrdersClient(channel);                          // cheap: make as many as you like
var order = await client.GetOrderAsync(new GetOrderRequest { OrderId = 42 }, cancellationToken: ct);
```

```csharp title="Program.cs: a typed client from DI"
builder.Services.AddGrpcClient<Orders.OrdersClient>(o => o.Address = new Uri("http://localhost:5287"))
    .EnableCallContextPropagation();   // in a gRPC service: pass on the deadline and cancellation
```

## Streams

```csharp
// Server streaming: read until the server ends the stream.
using var call = client.TrackOrder(new TrackOrderRequest { OrderId = 42 }, cancellationToken: ct);
await foreach (var update in call.ResponseStream.ReadAllAsync(ct)) { /* … */ }

// Client streaming: write, complete, then await the one reply.
using var import = client.ImportOrders(cancellationToken: ct);
await import.RequestStream.WriteAsync(request, ct);
await import.RequestStream.CompleteAsync();
var summary = await import;
```

```csharp title="Service side"
public override async Task TrackOrder(
    TrackOrderRequest request, IServerStreamWriter<OrderStatusUpdate> responseStream, ServerCallContext context)
{
    await foreach (var status in tracker.TrackAsync(request.OrderId, context.CancellationToken))
    {
        await responseStream.WriteAsync(new OrderStatusUpdate { Status = status }, context.CancellationToken);
    }
}   // returning ends the stream
```

## Deadlines and cancellation

```csharp
await client.GetOrderAsync(request, deadline: DateTime.UtcNow.AddSeconds(5));   // UTC, always
// Past the deadline: RpcException with StatusCode.DeadlineExceeded.

// In the service: the token is cancelled when the deadline passes or the client cancels.
await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
var timeLeft = context.Deadline - DateTime.UtcNow;   // DateTime.MaxValue when there's no deadline
```

## Status codes

| Failure                          | Status code                            |
| -------------------------------- | -------------------------------------- |
| Bad input from the caller        | `InvalidArgument`                      |
| No such thing                    | `NotFound`                             |
| Already exists                   | `AlreadyExists`                        |
| Not allowed in the current state | `FailedPrecondition`                   |
| Not signed in / not allowed      | `Unauthenticated` / `PermissionDenied` |
| Out of quota or capacity         | `ResourceExhausted`                    |
| Down for now: retry later        | `Unavailable`                          |
| Took too long                    | `DeadlineExceeded`                     |
| A bug on the server              | `Internal`                             |

An exception that isn't an `RpcException` reaches the client as `Unknown`, with the detail "Exception was thrown by handler.".

```csharp
try
{
    await client.GetOrderAsync(request, cancellationToken: ct);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
{
    // ex.Status.Detail is the server's message
}
```

## Rich error details

```xml
<PackageReference Include="Grpc.StatusProto" />   <!-- on both sides -->
```

```csharp title="Service side"
throw new Google.Rpc.Status
{
    Code = (int)Code.InvalidArgument,
    Message = "The order is invalid.",
    Details =
    {
        Any.Pack(new BadRequest
        {
            FieldViolations = { new BadRequest.Types.FieldViolation { Field = "customer_email", Description = "A customer email is required." } },
        }),
    },
}.ToRpcException();
```

```csharp title="Client side"
catch (RpcException ex) when (ex.GetRpcStatus()?.GetDetail<BadRequest>() is { } badRequest)
{
    foreach (var violation in badRequest.FieldViolations) { /* violation.Field, violation.Description */ }
}
```

## Health checks

```csharp title="Program.cs"
// Package: Grpc.AspNetCore.HealthChecks
builder.Services.AddGrpcHealthChecks()
    .AddCheck("warehouse", () => warehouse.IsUp ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
app.MapGrpcHealthChecksService();   // grpc.health.v1.Health: SERVING or NOT_SERVING
```
