# Design

## Context

- **The stub** `services/grpc.md` has a "What this covers" paragraph, two Learn links (the gRPC
  overview, and "Why migrate WCF to ASP.NET Core gRPC") and the "Coming soon" note. It's in
  `EXPECTED_SLUGS` and in the Learning path, after EF Core and before Testing. Neither it nor a
  cheatsheet is in `FULL_CONTENT_SLUGS` or `CHEATSHEETS`.
- **The conventions are set** in `site/README.md` and `samples/README.md`, as for EF Core: the topic
  skeleton, the cheatsheet shape, the exercise layout (`<Name>.Starter`, `<Name>.Solution`, a shared
  `tests/` compiled into `Starter.Tests` and `Solution.Tests`, both `.slnx` files), central package
  management, xUnit v3 (ADR-0003), and warnings as errors. `site/README.md` already allows a topic
  page to end its concepts with a "Coming from …" table for a library it replaces.
- **Decisions Emmz made** (2026-11-10): see proposal.md. In short, the extras are errors and status
  (with rich details) and health checks; exercise 08 is standalone with no Docker; it covers the
  server, streaming and deadlines, with the tests as the client; no command-line tool.
- **Current versions** (NuGet, 2026-11-10): the grpc-dotnet packages (`Grpc.AspNetCore`,
  `Grpc.Net.Client`, `Grpc.Net.ClientFactory`, `Grpc.Tools`, `Grpc.StatusProto`,
  `Grpc.AspNetCore.HealthChecks`) are all at 2.85.0. `Google.Protobuf` is at 3.36.2; 4.0.0 is an RC.
- **The samples' shared-tests layout meets code generation.** The shared tests need a client
  generated from the contract, but they compile against the Starter, whose `.proto` is unfinished
  until the engineer adds the streaming RPC. See D3.

## Goals / Non-Goals

**Goals:**
- An engineer finishes the page able to write a contract, host and call a service, stream, set and
  honour deadlines, and fail with the right status.
- Every behaviour the page states about status codes, deadlines and health checks is checked by
  running it, not written from memory.
- Exercise 08 runs with no Docker, like 01–06, and fails with messages that guide.

**Non-Goals:**
- A second app that calls the service. The page covers the client factory, and the tests are the
  client.
- TLS certificates for local development. The page runs over plain HTTP/2 locally and points to the
  Security page for TLS.
- A reusable gRPC test-helper project shared across exercises.

## Decisions

### D1. Page shape and order

The page follows the topic skeleton, with sections in this order:
1. **Outcome.**
2. **Cheatsheet tip.**
3. **The contract:** a `.proto` for the orders service, field numbers, types, well-known types
   (`Timestamp`, `Duration`, wrappers), money (D2), and evolving a contract (`reserved`).
4. **Code generation:** the `<Protobuf>` item with `GrpcServices="Server"` or `"Client"`, where the
   generated code goes, and that it runs on build.
5. **Hosting:** `AddGrpc`, `MapGrpcService<T>`, and HTTP/2 (D5).
6. **Calling:** `GrpcChannel.ForAddress`, the generated client, async calls, and `AddGrpcClient`
   (`Grpc.Net.ClientFactory`) for typed clients. A channel is expensive to create, so reuse it.
7. **Streaming:** server streaming in full; client and bidirectional streaming as shorter examples.
8. **Deadlines and cancellation:** `deadline:` on a call, `context.CancellationToken`, and
   `EnableCallContextPropagation` for onward calls.
9. **Errors and status:** a short table of status codes for common failures, `RpcException`, and
   rich details with `Grpc.StatusProto` (`BadRequest` field violations), read on the client with
   `GetRpcStatus()`. It also covers what an unhandled exception becomes (`Unknown`), and
   `EnableDetailedErrors` for development only.
10. **Health checks:** `AddGrpcHealthChecks` and `MapGrpcHealthChecksService`, reporting the app's
    ASP.NET Core health checks.
11. **"Coming from WCF":** a short table (D7).
12. **Pitfalls,** then **Exercise 08**.

Interceptors, JSON transcoding, gRPC-Web and server reflection each get one sentence and a Learn link,
in the section they're nearest to.

### D2. Money on the wire

Protobuf has no `decimal`. The page shows the two common options and picks one:
- **integer minor units** (`int64 unit_price_cents`): simple and exact, if every amount has the same
  scale;
- **a units-and-nanos message**, like Microsoft's `DecimalValue` example and Google's
  `google.type.Money`: exact, with any scale up to nine decimal places.

`double` is named as the wrong choice. **The exercise uses integer minor units** (`int64` cents),
because its prices are all in one currency with two decimal places, and it keeps the exercise's
mapping code to one line. That is an assumption made in this proposal; the page explains when the
units-and-nanos message is the better fit.

### D3. The contract the tests use

The tests can't use the client code generated in the Starter, because the Starter's `.proto` doesn't
define the streaming RPC until the engineer adds it, and the shared tests would then fail to compile.
So:
- **`tests/Protos/orders.proto` is the published contract.** The test projects generate a client from
  it (`GrpcServices="Client"`). The tests are a separate caller that has its own copy of the
  contract, which is how gRPC works between real services.
- **It differs from the Solution's `Protos/orders.proto` only in `option csharp_namespace`**, so the
  test assembly's generated types don't clash with the server's types, which it also sees through
  the project reference. The package, service, RPC and message names and the field numbers are
  identical, so the wire contract is the same. A check in §2 compares the two files with that one
  line removed.
- **The Starter's `.proto`** has the unary RPCs and their messages, and a `TODO` to add the streaming
  RPC and its messages to match the published contract. Before that, the server answers the
  streaming call with `Unimplemented`, which the tests turn into the guiding message.
- **Wrong field numbers** make the tests read garbage or defaults rather than fail outright. The
  streaming test's assertion message says to check names and field numbers against
  `tests/Protos/orders.proto`. This is a deliberate lesson: the wire uses numbers, not names.

**Rejected:**
- **generating the client in the Starter and Solution with `GrpcServices="Both"`**, because the
  shared tests wouldn't compile against an unfinished Starter;
- **giving the Starter the complete `.proto`**, because writing a contract is part of what the
  exercise teaches (Emmz's choice of scope).

### D4. Exercise 08 shape

**The contract** (package `orders.v1`, service `Orders`):
- `rpc PlaceOrder (PlaceOrderRequest) returns (PlaceOrderReply)`: a customer email and lines (SKU,
  quantity, unit price in cents). Returns the new order's id.
- `rpc GetOrder (GetOrderRequest) returns (Order)`: the order with its lines, total in cents, status
  and `google.protobuf.Timestamp placed_at`.
- `rpc TrackOrder (TrackOrderRequest) returns (stream OrderStatusUpdate)`: the one the engineer adds.

**Pre-written in both Starter and Solution:**
- `OrderStore`, a thread-safe in-memory store registered as a singleton;
- the request validation, which returns a list of problems, as in 07;
- `IOrderTracker`, a fake that yields an order's status updates (Placed, Picked, Packed, Shipped)
  with a configurable delay between them, taking a `CancellationToken`.

**The Starter's TODOs:**
1. Register gRPC and map `OrdersService` in `Program.cs`.
2. `PlaceOrder`: store the order and return its id. Fail with `InvalidArgument` when validation
   finds problems.
3. `GetOrder`: fail with `NotFound` for an unknown id, and map the stored order to the message.
4. Add `TrackOrder` and its messages to `Protos/orders.proto`, matching `tests/Protos/orders.proto`.
5. Implement `TrackOrder`: write each update to the response stream, and pass
   `context.CancellationToken` to the tracker.

**The Starter's `TrackOrder` route to "works, but badly"** (the 04 and 07 pattern): the Starter
can't implement an RPC that isn't in its contract yet, so there's no half-working version to give.
Instead, the hint for TODO 5 is in the comment, and the deadline test catches an implementation that
passes `CancellationToken.None`.

**The tests** (shared `tests/`, xUnit v3):
- a `WebApplicationFactory<Program>`, with a `GrpcChannel` built over the test server's handler
  (`HttpHandler = factory.Server.CreateHandler()`), so no port or HTTP/2 setup is needed;
- the factory swaps `IOrderTracker` for a test tracker with a short delay that records whether it was
  cancelled, and how many updates it produced;
- **not mapped:** every test first calls a helper that turns `Unimplemented` on `PlaceOrder`
  (or a 404 from the test server) into "register gRPC with `AddGrpc` and map `OrdersService` with
  `MapGrpcService`";
- **unary:** place and get back; each invalid case gives `InvalidArgument` and the store stays
  empty; an unknown id gives `NotFound`;
- **streaming:** all four updates arrive in order and the stream ends; an unknown id gives
  `NotFound`; `Unimplemented` gives the "add `TrackOrder` to the `.proto`" message;
- **deadline:** a deadline shorter than the updates take gives `DeadlineExceeded` on the client, and
  the test tracker records cancellation within a second of it.

**Revised in §2:**
- **The `Order` message has no `status`.** The store doesn't track status, only the tracker
  simulates it, so `GetOrder` would have had to hard-code `Placed`. `placed_at` is field 5.
- **The Starter's `PlaceOrder` works, but badly:** it stores every order, without validating. So
  TODO 2's tests fail with "it succeeded", the 04 and 07 pattern. `GetOrder` and `TrackOrder` aren't
  overridden in the Starter, so they answer `Unimplemented` with an empty detail.
- **The Starter's `OrdersService` takes only `OrderStore`.** An unread primary-constructor parameter
  (CS9113) is a build error here, so TODO 5 says to add `IOrderTracker`.
- **A cancel test** joins the deadline test: the client cancels after the first update, gets
  `Cancelled`, and the tracker must see its token cancelled. The requirement names both.
- **The test channel gives the server a later deadline.** The client sends its deadline in the
  `grpc-timeout` header, and both ends enforce it. In 1 of about 30 runs the server's timer fired
  first, and the in-memory test server reported the server's reset as `Unavailable` ("The
  application reset the request with error code 8"). A `DelegatingHandler` in `OrdersApp` adds a
  second to the header, so the client's timer always ends the call, with `DeadlineExceeded`. The
  service is still cancelled, by the client's abort. After the fix: 40 runs out of 40 passed, 4 at
  a time.
- **Field numbers are checked by behaviour:** with `order_id` and `status` swapped in the Starter's
  `OrderStatusUpdate`, the streaming test fails with the field-number hint ("the wire carries
  numbers, not names").

### D5. HTTP/2 locally

gRPC needs HTTP/2. The page's hosting example runs over plain HTTP (h2c) on a fixed local port, with
the Kestrel endpoint's protocol set to `Http2` in `appsettings.Development.json`, because HTTP/2
without TLS can't negotiate from HTTP/1.1. The page names the error a client gets when the endpoint
is HTTP/1.1 only, as a pitfall. The probe (task 1.1) checks the exact settings and error text on
.NET 10. TLS and the dev certificate are left to the Security page (proposal: Out of scope).

### D6. Cheatsheet

It follows the cheatsheet shape (back link, `##` groups of copyable blocks, minimal prose):
- a `.proto` skeleton (service, unary and streaming RPCs, a message, `reserved`, a well-known type);
- the `<Protobuf>` items for a server and a client, and the package references;
- server registration and a service class;
- client calls: channel, unary, reading a server stream, `AddGrpcClient`;
- deadlines and cancellation;
- throwing and catching `RpcException`, and the status codes table in short;
- rich error details on both sides;
- health checks;
- the Kestrel HTTP/2 setting.

### D7. The "Coming from WCF" table

Rows, each a WCF habit and what to do in gRPC:
- `[ServiceContract]` / `[OperationContract]` → a `service` and its `rpc`s in the `.proto`;
- `[DataContract]` / `[DataMember]` → `message`s, where field numbers go on the wire, not names;
- bindings and `<system.serviceModel>` XML → HTTP/2 on Kestrel, configured in code and
  `appsettings.json`;
- `svcutil` and "Add Service Reference" → `Grpc.Tools` generates the client on build from the
  `.proto`;
- `ChannelFactory<T>` and `ClientBase<T>` → `GrpcChannel` and the generated client, or
  `AddGrpcClient`;
- `FaultException<T>` → `RpcException` with a status code and rich details;
- duplex callback contracts → bidirectional streaming;
- `[OperationContract(IsOneWay = true)]` → no direct equivalent: a unary call that returns
  `google.protobuf.Empty`, or a client stream.

A sentence after the table says CoreWCF runs existing WCF services on .NET 10 for a lift-and-shift,
and that this primer teaches gRPC for new services. The stub's existing Learn link on migrating WCF
stays.

### D8. Packages

**`PackageVersion` entries** for `samples/Directory.Packages.props`:
- `Grpc.AspNetCore` 2.85.0, for the Starter and Solution (it brings `Grpc.Tools` and
  `Google.Protobuf` for server code generation);
- `Grpc.Net.Client` 2.85.0, `Grpc.Tools` 2.85.0 (as `PrivateAssets="all"`) and `Google.Protobuf`
  3.36.2, for the test projects' client code generation.

The packages used only on the page (`Grpc.Net.ClientFactory`, `Grpc.StatusProto`,
`Grpc.AspNetCore.HealthChecks`) aren't added to the samples. Their snippets are compiled in a
scratch project (D9). **If the 1.2 restore reports a vulnerable transitive package**, stop and ask,
as with the Pkcs pin in EF Core.

No `docs/dependencies.md` row is needed: `Grpc.Tools` ships `protoc` and the gRPC plugin inside the
NuGet package.

### D9. Code accuracy and verification

As in earlier changes:
- every snippet is compiled in a scratch project, net10.0, with warnings as errors;
- "from the exercise" snippets are copies of the real files;
- Learn links are checked with the link script.

Also, in a scratch app with a scratch client:
- **the deadline example** is run, to show the real `DeadlineExceeded` status and that the server's
  token is cancelled;
- **the error examples** are run: `InvalidArgument` with `BadRequest` details read back on the
  client, and an unhandled exception arriving as `Unknown`;
- **the health check** is called with the generated health client, showing `SERVING` and then
  `NOT_SERVING` after a check fails;
- **the HTTP/1.1-only endpoint error** (D5) is captured as the client sees it.

### D10. Section order

1. Probe and package versions (D3, D5, D8).
2. Exercise 08.
3. The cheatsheet, then the gRPC page.
4. The samples README.
5. The human check.

As before, the exercise comes before the page, so the page's "from the exercise" snippets are real.

## Risks / Trade-offs

- **Two copies of the contract can drift.** → The §2 check compares them, minus the namespace line,
  and the section's self-review repeats it.
- **The test server's HTTP/2 isn't Kestrel's.** The in-process handler doesn't exercise the HTTP/2
  setup the page teaches. → The probe runs the page's hosting example on Kestrel with a real client
  (D9).
- **Deadline tests depend on timing.** → The test tracker's delay is long relative to the deadline
  (for example, a 200 ms deadline against 1 s between updates), and the cancellation check allows a
  second. The tests are run three times in §2 to check they're stable.
- **`Google.Protobuf` 4.0 may ship soon.** → Pin 3.36.2 now. A later change can move up.
- **Money as cents** is an assumption (D2). → The page explains the alternative, and the exercise
  can change without touching the specs.
