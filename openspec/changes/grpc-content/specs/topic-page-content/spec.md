## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. That means the ASP.NET Core entry page, the Configuration, Dependency Injection and
Middleware pages and their three cheatsheets, the .NET 10 and `dotnet` CLI page with the `dotnet`
CLI cheatsheet, the Logging, Tracing and Metrics pages with their three cheatsheets, the EF Core
page with its cheatsheet, and the gRPC page with its cheatsheet. If one of them shows the
placeholder, the site test gate SHALL fail.

#### Scenario: Reader opens a finished page
- **WHEN** a reader opens the Dependency Injection page
- **THEN** the page has no "Coming soon" note

#### Scenario: Reader opens the finished dotnet CLI cheatsheet
- **WHEN** a reader opens the `dotnet` CLI cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished Observability page
- **WHEN** a reader opens the Logging, Tracing or Metrics page, or its cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished EF Core page
- **WHEN** a reader opens the EF Core page or the EF Core cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished gRPC page
- **WHEN** a reader opens the gRPC page or the gRPC cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

### Requirement: gRPC page covers contracts and code generation
The gRPC page SHALL teach contract-first services. It SHALL cover:
- a `.proto` file's services, messages, field numbers, scalar types and well-known types;
- that protobuf has no `decimal`, and how to send money instead;
- changing a contract without breaking callers;
- generating server and client code in the build with `Grpc.Tools`.

#### Scenario: Reader adds a field to a message
- **WHEN** a reader follows the page's advice to add a field to a message that callers already use
- **THEN** the new field gets an unused number, and old callers still decode the message

#### Scenario: Reader removes a field
- **WHEN** a reader removes a field from a message
- **THEN** the page says to mark its number and name `reserved`, so neither is reused

#### Scenario: Reader sends a price
- **WHEN** a reader needs a money amount in a message
- **THEN** the page says there is no `decimal` type, and shows a representation that keeps the exact
  value

### Requirement: gRPC page covers hosting and calling
The gRPC page SHALL cover hosting a service in ASP.NET Core with `AddGrpc` and `MapGrpcService`, and
why it needs HTTP/2. It SHALL cover calling a service through a `GrpcChannel` and the generated
client, and registering a typed client with the client factory, with the reason to reuse channels.

#### Scenario: Reader runs a service locally
- **WHEN** a reader follows the page's hosting example and starts the app
- **THEN** the service answers gRPC calls over HTTP/2 on the local endpoint the page gives

#### Scenario: Reader calls a service from another app
- **WHEN** a reader registers the page's typed client in an ASP.NET Core app
- **THEN** it can inject the generated client and call the service, without building a channel per
  call

### Requirement: gRPC page covers streaming, deadlines and cancellation
The gRPC page SHALL cover server, client and bidirectional streaming. It SHALL cover setting a
deadline on a call, honouring the call's cancellation token in a service, and propagating a deadline
to the calls a service makes in turn.

#### Scenario: Reader reads a server stream
- **WHEN** a reader follows the page's server-streaming example
- **THEN** the client reads each message as the server writes it, until the server ends the stream

#### Scenario: Deadline passes during a call
- **WHEN** a client's deadline passes while the service is still working
- **THEN** the page shows that the client gets `DeadlineExceeded`, and that a service honouring its
  cancellation token stops its work

### Requirement: gRPC page covers errors and status
The gRPC page SHALL cover gRPC status codes and which to use for common failures, throwing and
catching `RpcException`, and sending rich error details, such as which fields of a request were
invalid.

#### Scenario: Reader reports invalid input
- **WHEN** a reader's service rejects a request with an invalid field
- **THEN** the page shows it failing with `InvalidArgument`, with error details that name the field,
  and the client reading those details

#### Scenario: Unhandled exception in a service
- **WHEN** a service method throws an exception that isn't an `RpcException`
- **THEN** the page says the client gets `Unknown`, without the exception's details

### Requirement: gRPC page covers health checks
The gRPC page SHALL cover the standard gRPC health service, reporting the app's ASP.NET Core health
checks, so that orchestrators and load balancers can probe a gRPC app.

#### Scenario: Reader adds a health service
- **WHEN** a reader follows the page's health-check example
- **THEN** a health check call returns `SERVING` while the app's health checks pass, and
  `NOT_SERVING` when one fails

### Requirement: gRPC page maps WCF habits
The gRPC page SHALL have a "Coming from WCF" section, after its concepts and before its Pitfalls. It
SHALL map these WCF habits to gRPC: service and operation contracts, data contracts, bindings and
XML configuration, generated proxies, `FaultException`, and duplex callback contracts. It SHALL say
that CoreWCF exists for moving WCF services as they are.

#### Scenario: Engineer coming from WCF looks for data contracts
- **WHEN** an engineer used to `[DataContract]` and `[DataMember]` reads the mapping
- **THEN** it says that messages are defined in the `.proto`, and that field numbers, not member
  names, are what goes on the wire

#### Scenario: Engineer coming from WCF looks for faults
- **WHEN** an engineer used to `FaultException<T>` reads the mapping
- **THEN** it says to fail with a status code and rich error details, and links to the page's
  errors section
