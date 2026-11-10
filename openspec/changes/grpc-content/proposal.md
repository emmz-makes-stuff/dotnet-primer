# Proposal

## Why

gRPC is the next page on the learning path, after EF Core, and it is still a placeholder. Emmz chose
it as the next change on 2026-11-10. The engineers come from .NET 3.5, where service-to-service calls
meant WCF (or ASMX and Remoting before it). Their WCF habits don't carry over: attribute contracts,
XML config bindings, `svcutil` proxies, `FaultException<T>` and duplex callbacks. A migration that
replaces WCF services needs gRPC's contract-first model, its HTTP/2 hosting, and its error and
deadline semantics.

## What Changes

- **Write the full gRPC page** (`services/grpc`), replacing the stub. It follows the stub's outline,
  plus the two extras Emmz picked on 2026-11-10 (error handling and status, and health checks; not
  interceptors, JSON transcoding or reflection):
  - **The contract:** `.proto` syntax, messages and field numbers, scalar and well-known types, why
    there is no `decimal` (and what to use), and evolving a contract without breaking callers
    (`reserved`, never reusing a field number).
  - **Code generation:** `Grpc.Tools` and the `<Protobuf>` item, server and client stubs.
  - **Hosting:** `AddGrpc`, `MapGrpcService<T>`, and HTTP/2 on Kestrel.
  - **Calling:** `GrpcChannel`, the generated client, and typed clients through the client factory
    (`AddGrpcClient`).
  - **Streaming:** server, client and bidirectional streaming.
  - **Deadlines and cancellation:** setting a deadline, honouring `ServerCallContext.CancellationToken`,
    and propagating deadlines to onward calls.
  - **Errors and status:** status codes, `RpcException`, and rich error details (`Grpc.StatusProto`).
  - **Health checks:** the standard gRPC health service (`Grpc.AspNetCore.HealthChecks`), wired to
    ASP.NET Core health checks.
  - **Coming from WCF:** a short table mapping WCF habits to gRPC, after the concepts and before the
    Pitfalls, as the EF Core page does for EF6.
  - **Pitfalls,** then **Exercise 08**.
- **A gRPC cheatsheet** (`cheatsheets/grpc`): a `.proto` skeleton, the `<Protobuf>` items, server
  registration, client calls and `AddGrpcClient`, the streaming call shapes, deadlines, throwing and
  catching status errors, and health checks. It is linked both ways with the page, listed in the
  sidebar and on the introduction, and added to the site tests' maps.
- **Exercise 08** (`samples/exercises/08-grpc`): a starter, a solution and shared tests, like 01–07.
  It is **standalone, with an in-memory order store, so it needs no Docker** (Emmz, 2026-11-10). The
  engineer:
  - registers gRPC and maps the orders service;
  - implements placing an order and getting one, with the right status codes for invalid input and
    an unknown order;
  - adds a server-streaming RPC to the `.proto` that sends an order's status updates;
  - implements it so that it stops when the client's deadline passes or the call is cancelled.

  **The tests act as the client** (Emmz, 2026-11-10: server, streaming and deadlines; no separate
  client app). They call the service in process through `WebApplicationFactory` and a `GrpcChannel`.
- **The samples README:** the layout and exercise table gain 08.
- **No command-line tool** for calling services by hand (Emmz, 2026-11-10: no grpcurl). The page and
  exercise make calls from C# only.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" adds the gRPC page and its cheatsheet.
  - New: "gRPC page covers contracts and code generation", "… covers hosting and calling",
    "… covers streaming, deadlines and cancellation", "… covers errors and status",
    "… covers health checks", and "gRPC page maps WCF habits".
- **`site-structure`:** "Cheatsheets per area" grows from eight cheatsheets to nine.
- **`sample-exercises`:**
  - "Site describes each exercise" gains a scenario for exercise 08.
  - New: "gRPC exercise practises the contract and unary calls", and "gRPC exercise practises
    streaming and deadlines".

## Impact

- **Site:**
  - `site/src/content/docs/services/grpc.md`, and a new `site/src/content/docs/cheatsheets/grpc.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`).
- **Samples:**
  - `samples/exercises/08-grpc`, and both `.slnx` files;
  - new `PackageVersion` entries in `samples/Directory.Packages.props`: `Grpc.AspNetCore`,
    `Grpc.Net.Client` and `Grpc.Tools` at 2.85.0, and `Google.Protobuf` at 3.36.2 (the newest stable
    versions on 2026-11-10; Protobuf 4.0 is still an RC);
  - `samples/README.md`.
- **Tooling:** none new. Code generation runs inside the build through `Grpc.Tools`, so
  `docs/dependencies.md` and `make deps` don't change.
- **Out of scope:**
  - interceptors, JSON transcoding, gRPC-Web and server reflection, beyond a sentence each saying
    they exist;
  - grpcurl and other command-line clients;
  - authentication and TLS certificates beyond running locally, which belong to the Security page;
  - client-side load balancing and retries policy, beyond a sentence;
  - CoreWCF, beyond a sentence in the WCF section saying it exists for lift-and-shift;
  - gRPC's OpenTelemetry instrumentation.
