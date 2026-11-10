---
title: "gRPC"
description: "Contract-first RPC with Protocol Buffers: .proto contracts and code generation, hosting and calling services in ASP.NET Core, streaming, deadlines and cancellation, status codes and rich errors, health checks, and coming from WCF."
learn:
  - title: "gRPC on .NET overview"
    url: https://learn.microsoft.com/aspnet/core/grpc/
  - title: "Create Protobuf messages for .NET apps"
    url: https://learn.microsoft.com/aspnet/core/grpc/protobuf
  - title: "gRPC client factory integration in .NET"
    url: https://learn.microsoft.com/aspnet/core/grpc/clientfactory
  - title: "Reliable gRPC services with deadlines and cancellation"
    url: https://learn.microsoft.com/aspnet/core/grpc/deadlines-cancellation
  - title: "Error handling with gRPC"
    url: https://learn.microsoft.com/aspnet/core/grpc/error-handling
  - title: "gRPC health checks in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/grpc/health-checks
  - title: "Why migrate WCF to ASP.NET Core gRPC"
    url: https://learn.microsoft.com/aspnet/core/grpc/why-migrate-wcf-to-dotnet-grpc
---

By the end of this page you'll be able to build a gRPC service in ASP.NET Core and call it: write the contract in a `.proto` file and change it without breaking callers, host the service over HTTP/2, call it with a generated client, stream messages in either direction, set and honour deadlines, fail with the status code that fits, and report the app's health.

:::tip[Cheatsheet]
Keep the [gRPC cheatsheet](/cheatsheets/grpc/) open while you work.
:::

The examples use the orders from the [EF Core](/data/ef-core/) page, as a service other apps call: place an order, get one back, and track it through the warehouse.

## The contract

gRPC is contract first. The service and its messages are written in a `.proto` file, in the Protocol Buffers language, and both the server and every client generate their code from it.

```proto title="Protos/orders.proto"
syntax = "proto3";

option csharp_namespace = "GrpcOrders.Contract";

package orders.v1;

import "google/protobuf/timestamp.proto";

// Places, reads and tracks orders.
service Orders {
  // Places an order and returns its id.
  rpc PlaceOrder (PlaceOrderRequest) returns (PlaceOrderReply);

  // Gets one order by its id.
  rpc GetOrder (GetOrderRequest) returns (Order);

  // Streams an order's status updates, one by one, until it has shipped.
  rpc TrackOrder (TrackOrderRequest) returns (stream OrderStatusUpdate);
}

message PlaceOrderRequest {
  string customer_email = 1;
  repeated OrderLine lines = 2;
}

message OrderLine {
  string sku = 1;
  int32 quantity = 2;
  // Money in minor units (cents). Protobuf has no decimal type, and a double isn't exact.
  int64 unit_price_cents = 3;
}

message Order {
  int32 id = 1;
  string customer_email = 2;
  repeated OrderLine lines = 3;
  int64 total_cents = 4;
  google.protobuf.Timestamp placed_at = 5;
}
```

This is part of exercise 08's contract. The things to know:

- **Field numbers are the wire format.** A message is sent as numbered fields, not named ones: `customer_email = 2` means "field 2 is a string". Names only matter to the generated code.
- **Scalar types** are `int32`, `int64`, `bool`, `string`, `bytes`, `double` and a few more. `repeated` is a list. In C#, a repeated field is a read-only property: add to it with `Add` or `AddRange`.
- **Proto3 has no null** for scalars and strings. A field the caller didn't set arrives as its default: `0`, `""` or an empty list. Check for empty, not for null.
- **Well-known types** cover the common cases: `google.protobuf.Timestamp` and `Duration` for time, and wrappers such as `Int32Value` when you need to tell "not set" from zero. `Timestamp.FromDateTimeOffset` and `ToDateTimeOffset` convert in C#.
- **`option csharp_namespace`** sets the C# namespace and is never sent. `package` is: it's part of every call's path, `/orders.v1.Orders/GetOrder`.

### Money

There's no `decimal`. Two representations keep an exact value:

- **Minor units in an `int64`:** `unit_price_cents = 1024` for 10.24. Simple, when every amount has the same number of decimal places. The exercise does this.
- **Units and nanos:** a message with an `int64` for the whole units and an `int32` for the billionths, as Google's `google.type.Money` and Learn's [`DecimalValue` example](https://learn.microsoft.com/aspnet/core/grpc/protobuf#decimals) do. Use it for mixed scales or currencies.

```proto
message Money {
  string currency_code = 1;
  int64 units = 2;
  int32 nanos = 3;
}
```

Never a `double`: 0.1 + 0.2 isn't 0.3 in binary floating point.

### Changing a contract

Callers you don't control keep their old copy of the contract, so a change must still decode on both sides.

- **Adding a field is safe.** Give it a new number. Old callers skip a field they don't know, and new code reading an old message sees the default.
- **Removing a field:** delete it and mark its number and name `reserved`, so nobody reuses them later with another type.

  ```proto
  message Order {
    reserved 6;
    reserved "status";
    // …
  }
  ```

- **Never change a field's number or type,** and never reuse a number. A caller still sending the old field would be read as the new one.
- **Renaming** a field is safe on the wire, since only the number is sent, but it breaks the code of every caller that regenerates from the new contract.

For a change that breaks callers, add a new package version (`orders.v2`) next to the old one.

## Code generation

The `Grpc.Tools` package runs the Protocol Buffers compiler during the build. A `<Protobuf>` item says which `.proto` to compile, and which side to generate:

```xml title="Grpc.Solution.csproj"
<ItemGroup>
  <!-- Grpc.Tools, inside Grpc.AspNetCore, generates the service base class from the contract on build. -->
  <Protobuf Include="Protos/orders.proto" GrpcServices="Server" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Grpc.AspNetCore" />
</ItemGroup>
```

- **`GrpcServices="Server"`** generates the messages and an `Orders.OrdersBase` class to override.
- **`GrpcServices="Client"`** generates the messages and an `Orders.OrdersClient`. A client needs `Grpc.Net.Client`, `Google.Protobuf`, and `Grpc.Tools` with `PrivateAssets="all"`, since it's only used at build time.
- **`GrpcServices="Both"`** for an app that serves one service and calls another.

The generated code goes in `obj/`, and is rebuilt whenever the `.proto` changes. You never edit it, and it isn't in source control: the `.proto` is.

## Hosting a service

A service is a class that overrides the generated base, one method per RPC:

```csharp title="OrdersService.cs"
public sealed class OrdersService(OrderStore store, IOrderTracker tracker) : Orders.OrdersBase
{
    public override Task<PlaceOrderReply> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
    {
        // A failed call is an RpcException with a status code, not a reply with an error field.
        if (request.Problems() is { Count: > 0 } problems)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(" ", problems)));
        }

        var order = store.Add(
            request.CustomerEmail,
            [.. request.Lines.Select(l => new StoredLine(l.Sku, l.Quantity, l.UnitPriceCents))]);
        return Task.FromResult(new PlaceOrderReply { OrderId = order.Id });
    }

    // GetOrder and TrackOrder follow.
}
```

Register gRPC, then map the service. Each call creates a new `OrdersService`, like a controller per request, so it can take scoped services:

```csharp title="Program.cs"
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<OrdersService>();
```

### HTTP/2

gRPC needs HTTP/2. Over HTTPS, Kestrel and the client agree on HTTP/2 during the TLS handshake. Over plain HTTP, which is what you use locally, they can't negotiate, so the endpoint must serve HTTP/2 only:

```json title="appsettings.json"
{
  "Kestrel": {
    "Endpoints": {
      "Grpc": { "Url": "http://localhost:5287", "Protocols": "Http2" }
    }
  }
}
```

Any free port works; change 5287 if something else has it. TLS certificates belong to the [Security](/security/) page. A browser can't call gRPC directly; [gRPC-Web](https://learn.microsoft.com/aspnet/core/grpc/grpcweb) and [JSON transcoding](https://learn.microsoft.com/aspnet/core/grpc/json-transcoding) exist for that.

## Calling a service

A client is generated from the contract. It sends its calls through a channel to one address:

```csharp
using var channel = GrpcChannel.ForAddress("http://localhost:5287");
var client = new Orders.OrdersClient(channel);

var order = await client.GetOrderAsync(new GetOrderRequest { OrderId = 42 }, cancellationToken: ct);
```

**A channel is expensive to create** (it holds HTTP/2 connections) and safe to share, so create one per address and reuse it. Clients are cheap: make as many as you like over the same channel.

In an ASP.NET Core app, register a typed client with the client factory instead, and inject it where you need it. It comes with `Grpc.AspNetCore`, or `Grpc.Net.ClientFactory` on its own:

```csharp title="Program.cs"
builder.Services.AddGrpcClient<Orders.OrdersClient>(o => o.Address = new Uri("http://localhost:5287"));
```

```csharp
app.MapGet("/orders/{id:int}/email", async (int id, Orders.OrdersClient orders, CancellationToken ct) =>
    (await orders.GetOrderAsync(new GetOrderRequest { OrderId = id }, cancellationToken: ct)).CustomerEmail);
```

The factory manages the channel and its connections for you. [Retries](https://learn.microsoft.com/aspnet/core/grpc/retries) and [client-side load balancing](https://learn.microsoft.com/aspnet/core/grpc/loadbalancing) are channel settings too. For cross-cutting work on every call, such as logging, both sides have [interceptors](https://learn.microsoft.com/aspnet/core/grpc/interceptors), gRPC's middleware.

## Streaming

A call can stream messages in either direction, or both. Each stream is one long HTTP/2 request, so messages arrive as they're written.

**Server streaming:** one request, many replies. The service writes each message, and returning ends the stream. This is `TrackOrder` from exercise 08:

```csharp title="OrdersService.cs"
public override async Task TrackOrder(
    TrackOrderRequest request, IServerStreamWriter<OrderStatusUpdate> responseStream, ServerCallContext context)
{
    if (store.Find(request.OrderId) is null)
    {
        throw NotFound(request.OrderId);
    }

    // The call's token is cancelled when the client's deadline passes or it cancels the call.
    // Pass it to everything that waits, so the work stops then, not when it would have finished.
    await foreach (var status in tracker.TrackAsync(request.OrderId, context.CancellationToken))
    {
        await responseStream.WriteAsync(
            new OrderStatusUpdate { OrderId = request.OrderId, Status = status, At = Timestamp.FromDateTime(DateTime.UtcNow) },
            context.CancellationToken);
    }

    // Returning ends the stream: the client's read loop finishes.
}
```

The client reads until the stream ends:

```csharp
using var call = client.TrackOrder(new TrackOrderRequest { OrderId = 42 }, cancellationToken: ct);
await foreach (var update in call.ResponseStream.ReadAllAsync(ct))
{
    Console.WriteLine($"{update.OrderId}: {update.Status}");
}
```

**Client streaming:** many requests, one reply. In the contract, `rpc ImportOrders (stream PlaceOrderRequest) returns (ImportSummary);`. The client writes, says it's finished, then awaits the reply:

```csharp
using var call = client.ImportOrders(cancellationToken: ct);
foreach (var order in orders)
{
    await call.RequestStream.WriteAsync(order, ct);
}

await call.RequestStream.CompleteAsync();
var summary = await call;
```

The service reads with `await foreach (var order in requestStream.ReadAllAsync(context.CancellationToken))`, and returns the reply once the client completes.

**Bidirectional streaming:** both sides stream at once, independently. In the contract, `rpc WatchOrders (stream TrackOrderRequest) returns (stream OrderStatusUpdate);`. The client reads on one task while it writes on another:

```csharp
using var call = client.WatchOrders(cancellationToken: ct);
var reading = Task.Run(async () =>
{
    await foreach (var update in call.ResponseStream.ReadAllAsync(ct))
    {
        Console.WriteLine($"{update.OrderId}: {update.Status}");
    }
});

foreach (var id in orderIds)
{
    await call.RequestStream.WriteAsync(new TrackOrderRequest { OrderId = id }, ct);
}

await call.RequestStream.CompleteAsync();
await reading;
```

## Deadlines and cancellation

A call without a deadline can wait forever. Give every call one: the latest time, in UTC, by which you want an answer.

```csharp
var order = await client.GetOrderAsync(
    new GetOrderRequest { OrderId = 42 }, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: ct);
```

When the deadline passes, the call fails with `StatusCode.DeadlineExceeded`, and the deadline travels with the call, so the server knows it too. In the service, `context.CancellationToken` is cancelled when the deadline passes or the client cancels. **Pass it to everything that waits:** database queries, HTTP calls, `Task.Delay`, stream writes. Otherwise the service keeps working for a caller that has gone. `context.Deadline` is the deadline itself, or `DateTime.MaxValue` when there isn't one.

A service that calls another service should pass its own deadline on, so the whole chain gives up together. With the client factory, that's one line:

```csharp title="Program.cs"
builder.Services.AddGrpcClient<Inventory.InventoryClient>(o => o.Address = new Uri("http://localhost:5287"))
    .EnableCallContextPropagation();
```

Inside a gRPC call, every call through that client then gets the incoming call's deadline and cancellation token. With a 5-second deadline on `PlaceOrder`, the onward `Reserve` call saw 4.999 s left.

## Errors and status

A gRPC call succeeds or fails with a **status code** and a detail message. On the server, fail by throwing an `RpcException`. On the client, the same `RpcException` is thrown from the call:

```csharp
try
{
    var order = await client.GetOrderAsync(new GetOrderRequest { OrderId = 7 }, cancellationToken: ct);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
{
    Console.WriteLine(ex.Status.Detail);   // There's no order 7.
}
```

Pick the code that tells the caller what to do next:

| Failure                                         | Status code                            |
| ----------------------------------------------- | -------------------------------------- |
| The request is wrong, so retrying won't help    | `InvalidArgument`                      |
| The thing asked for doesn't exist               | `NotFound`                             |
| It already exists                               | `AlreadyExists`                        |
| The system isn't in a state that allows it      | `FailedPrecondition`                   |
| The caller isn't signed in, or isn't allowed    | `Unauthenticated` / `PermissionDenied` |
| Out of quota or capacity                        | `ResourceExhausted`                    |
| The service is down for now: retry with backoff | `Unavailable`                          |
| The deadline passed                             | `DeadlineExceeded`                     |
| A bug in the service                            | `Internal`                             |

**An exception that isn't an `RpcException`** reaches the client as `Unknown`, with only the detail "Exception was thrown by handler.". The exception and its stack trace are logged on the server, not sent. `EnableDetailedErrors` adds the exception's message to the detail, which helps in development and leaks internals in production:

```csharp title="Program.cs"
builder.Services.AddGrpc(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());
```

### Rich error details

A detail string is for people. To tell a client _which_ fields were wrong, send structured details as well. The `Grpc.StatusProto` package (on both sides) adds Google's standard error model, with message types such as `BadRequest`, `ErrorInfo` and `RetryInfo`:

```csharp title="OrdersService.cs"
throw new Google.Rpc.Status
{
    Code = (int)Code.InvalidArgument,
    Message = "The order is invalid.",
    Details =
    {
        Any.Pack(new BadRequest
        {
            FieldViolations =
            {
                new BadRequest.Types.FieldViolation { Field = "customer_email", Description = "A customer email is required." },
            },
        }),
    },
}.ToRpcException();
```

The client unpacks them from the exception:

```csharp
catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
{
    var status = ex.GetRpcStatus();
    foreach (var violation in status?.GetDetail<BadRequest>()?.FieldViolations ?? [])
    {
        Console.WriteLine($"{violation.Field}: {violation.Description}");   // customer_email: A customer email is required.
    }
}
```

## Health checks

gRPC has a standard health service, `grpc.health.v1.Health`, that orchestrators and load balancers call to decide whether to send an instance traffic. The `Grpc.AspNetCore.HealthChecks` package serves it from the app's ordinary ASP.NET Core health checks:

```csharp title="Program.cs"
builder.Services.AddGrpcHealthChecks()
    .AddCheck("warehouse", () => warehouse.IsUp
        ? HealthCheckResult.Healthy()
        : HealthCheckResult.Unhealthy("The warehouse is down."));

var app = builder.Build();

app.MapGrpcHealthChecksService();
```

A `Check` call answers `SERVING` while every check passes, and `NOT_SERVING` as soon as one fails. Kubernetes can probe it directly with a [gRPC probe](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/#define-a-grpc-liveness-probe). Tools such as grpcurl can also discover a service's methods if you turn on [server reflection](https://learn.microsoft.com/aspnet/core/grpc/test-tools).

## Coming from WCF

| In WCF                                         | In gRPC                                                                                     |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------- |
| `[ServiceContract]` and `[OperationContract]`  | A `service` and its `rpc`s in the `.proto`                                                  |
| `[DataContract]` and `[DataMember]`            | `message`s, where the field numbers go on the wire, not the member names                    |
| Bindings and `<system.serviceModel>` XML       | HTTP/2 on Kestrel, configured in code and `appsettings.json`                                |
| `svcutil` or "Add Service Reference"           | `Grpc.Tools` generates the client on every build, from the `.proto`                         |
| `ChannelFactory<T>` or a `ClientBase<T>` proxy | `GrpcChannel` and the generated client, or `AddGrpcClient`                                  |
| `FaultException<T>`                            | An `RpcException` with a [status code and rich details](#errors-and-status)                 |
| Duplex callback contracts                      | Bidirectional streaming                                                                     |
| `[OperationContract(IsOneWay = true)]`         | No direct equivalent: a unary call that returns `google.protobuf.Empty`, or a client stream |

To move existing WCF services to .NET 10 as they are, [CoreWCF](https://github.com/CoreWCF/CoreWCF) is a community port of WCF's server side. It's a lift-and-shift path; for new services, and services you're rewriting anyway, use gRPC. Learn's [WCF to gRPC guide](https://learn.microsoft.com/aspnet/core/grpc/why-migrate-wcf-to-dotnet-grpc) goes further.

## Pitfalls

- **Every call fails with `HTTP_1_1_REQUIRED`.** The client reports `StatusCode=Internal`, "The HTTP/2 server closed the connection. HTTP/2 error code 'HTTP_1_1_REQUIRED' (0xd)". The endpoint is plain HTTP with Kestrel's default protocols (`Http1AndHttp2`), which can't negotiate HTTP/2 without TLS, so it serves HTTP/1.1 only. Fix: `"Protocols": "Http2"` on that endpoint, or HTTPS.
- **Money as a `double`.** Amounts drift by fractions of a cent. Fix: minor units in an `int64`, or a units-and-nanos message.
- **Reusing or renumbering a field.** Callers with the old contract decode the wrong data, silently: no error, just wrong values. Fix: never change a field's number or type; `reserved` removed numbers and names.
- **A new channel for every call.** Each one opens new connections, which is slow and can exhaust sockets under load. Fix: one channel per address, reused, or the client factory.
- **No deadline.** A slow or stuck service holds the caller's request, and its resources, indefinitely. Fix: a deadline on every call, and `EnableCallContextPropagation` in services that call others.
- **`InvalidOperationException: Deadline must have a kind DateTimeKind.Utc`.** The deadline came from `DateTime.Now`, which is local time, and the call throws before it's sent. Fix: `DateTime.UtcNow.AddSeconds(…)`.
- **Ignoring `context.CancellationToken`.** The service keeps querying and writing after the caller has given up. Fix: pass the token to everything that waits.
- **Plain exceptions for expected failures.** The client gets `Unknown` and can't tell a missing order from a bug. Fix: throw `RpcException` with the status code that fits, and keep `EnableDetailedErrors` to development.
- **Checking a string for null.** Proto3 strings are never null: an unset field is `""`. Fix: `string.IsNullOrEmpty`, or a wrapper type such as `StringValue` when "not set" must differ from empty.

## Exercise 08: an orders service over gRPC

**Goal:** serve the orders as a gRPC service: register and map it, fail with the right status codes, add a streaming RPC to the contract, and make the stream stop when the caller's deadline passes or it cancels. The order store, the request validation and a pretend warehouse tracker are already written. The tests are the client: they call your service through the published contract, as another team's app would.

**Where:** [`samples/exercises/08-grpc`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/08-grpc) in the GitHub repo. Clone the repo, then work in the `Starter` project. **This exercise needs no Docker**: the store is in memory, and the tests run the app in process.

**Steps:**

1. In `Starter/Program.cs`, register gRPC with `AddGrpc` and map `OrdersService` (TODO 1).
2. In `Starter/OrdersService.cs`, make `PlaceOrder` reject invalid orders with `InvalidArgument` (TODO 2), and override `GetOrder`, failing with `NotFound` for an unknown id (TODO 3).
3. In `Starter/Protos/orders.proto`, add the `TrackOrder` server-streaming RPC and its two messages (TODO 4). Copy them from the published contract, `tests/Protos/orders.proto`: the names and field numbers must match.
4. Back in `OrdersService.cs`, override `TrackOrder` to write each status from the tracker to the response stream, passing `context.CancellationToken` all the way down (TODO 5).

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/08-grpc/Starter.Tests
```

If the tracking test shows statuses that are empty or for the wrong order, compare your field numbers with the published contract: the wire carries numbers, not names.

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
