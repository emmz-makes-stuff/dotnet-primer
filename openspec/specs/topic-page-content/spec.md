# topic-page-content Specification

## Purpose

Sets what a learning-path page with full content must contain and guarantee. An engineer moving off
.NET 3.5 should be able to learn a topic from that page alone, and then go to Microsoft Learn for
depth.

## Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page, a C# language page or a cheatsheet delivered with full content SHALL NOT show
the "Coming soon" placeholder note. If one of them shows the placeholder, the site test gate SHALL
fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection and Middleware pages and their three cheatsheets, the .NET 10 and `dotnet`
  CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages with their three
  cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its cheatsheet, the Testing
  page with its cheatsheet, the Security & auth page with its cheatsheet, the Async & performance
  page with its cheatsheet, the Containers page with its cheatsheet, the Orchestration & AWS page
  with its cheatsheet, and the four C# language pages with the C# cheatsheet

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

#### Scenario: Reader opens the finished Testing page
- **WHEN** a reader opens the Testing page or the Testing cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Security page
- **WHEN** a reader opens the Security & auth page or the Security cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Async page
- **WHEN** a reader opens the Async & performance page or the Async & performance cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Containers page
- **WHEN** a reader opens the Containers page or the Containers cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Orchestration page
- **WHEN** a reader opens the Orchestration & AWS page or the Orchestration & AWS cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished C# page
- **WHEN** a reader opens any of the four C# language pages, or the C# cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

### Requirement: Topic page structure
Each full-content topic page SHALL do the following, in this order:
- open by saying what the reader will be able to do afterwards;
- explain the concepts with code examples;
- end with a "Pitfalls" section of the common mistakes.

A topic page with a cheatsheet SHALL link to it near the top. A topic page with an exercise SHALL
describe the exercise after the pitfalls.

#### Scenario: Reading the Middleware page top to bottom
- **WHEN** a reader reads the Middleware page from the top
- **THEN** they see the outcome statement, a link to the Middleware cheatsheet, the concepts with
  examples, a Pitfalls section, and then the description of exercise 03

### Requirement: ASP.NET Core entry page coverage
The ASP.NET Core entry page SHALL explain:
- the host and `WebApplication`;
- the builder → build → run shape of `Program.cs`;
- minimal API endpoints, route parameters, route groups and typed results;
- when to choose controllers instead.

It SHALL introduce Configuration, Dependency Injection and Middleware as the next steps on the path, and
link to each.

#### Scenario: Reader starts the learning path
- **WHEN** a reader opens `/aspnet-core/`
- **THEN** the page walks through a minimal `Program.cs`, shows at least one minimal API endpoint with
  a route parameter, and links to the Configuration, Dependency Injection and Middleware pages

### Requirement: Configuration page covers sources and layering
The Configuration page SHALL explain:
- the default configuration providers, and that later providers override earlier ones;
- environments and `appsettings.{Environment}.json`;
- user secrets for local development;
- how hierarchical keys map to environment variables (`__`) and command-line arguments;
- reading values through `IConfiguration`.

#### Scenario: Reader needs to override a setting in a container
- **WHEN** a reader looks up how to override `Greeting:Message` without editing files
- **THEN** the page shows the environment variable `Greeting__Message` and explains why it wins over
  `appsettings.json`

### Requirement: Configuration page covers the options pattern
The Configuration page SHALL explain:
- binding configuration sections to typed classes with the options pattern;
- the difference between `IOptions`, `IOptionsSnapshot` and `IOptionsMonitor`;
- validating options at startup.

#### Scenario: Reader chooses an options interface
- **WHEN** a reader needs settings that pick up changes without restarting the app
- **THEN** the page says which options interface reloads per request, which notifies on change, and
  which reads values once

#### Scenario: Reader wants bad configuration to fail fast
- **WHEN** a reader looks up how to reject invalid options
- **THEN** the page shows options validation that runs at startup, so the app refuses to start rather
  than failing on the first request

### Requirement: Dependency Injection page coverage
The Dependency Injection page SHALL explain:
- registering services with the built-in container;
- the singleton, scoped and transient lifetimes, with when to use each;
- constructor and primary-constructor injection;
- injecting into minimal API handlers;
- registration patterns: interface to implementation, factory, multiple implementations, `TryAdd`, open
  generics and keyed services;
- captive dependencies and how scope validation catches them;
- creating a scope outside a request.

#### Scenario: Reader chooses a lifetime
- **WHEN** a reader is unsure whether a service should be singleton, scoped or transient
- **THEN** the page states, for each lifetime, how long an instance lives and gives a typical use

#### Scenario: Reader hits a captive dependency
- **WHEN** a reader injects a scoped service into a singleton
- **THEN** the page explains why that is a bug, and that the app fails fast at startup in Development
  because of scope validation

### Requirement: Middleware page coverage
The Middleware page SHALL explain:
- that each request passes through an ordered pipeline of middleware, and the response passes back out
  in reverse;
- `Use`, `Run` and `Map`;
- that registration order is execution order, with Microsoft's recommended order for the built-in
  middleware;
- writing middleware both inline and as a class;
- short-circuiting;
- exception handling that returns Problem Details;
- endpoint filters, and when to use one instead of middleware.

#### Scenario: Reader debugs a middleware order problem
- **WHEN** a reader's authorization runs before routing has selected an endpoint
- **THEN** the page's recommended-order list shows that routing comes before authentication and
  authorization, and explains why

### Requirement: Accurate .NET 10 code examples
C# examples on full-content pages SHALL be valid for .NET 10 (`net10.0`, C# 14). An example presented
as a complete `Program.cs` or file SHALL compile with warnings as errors in an ASP.NET Core web
project. A fragment SHALL be labelled with the file it belongs in, or SHALL be obviously partial.

#### Scenario: Engineer pastes a complete example
- **WHEN** an engineer pastes a complete `Program.cs` example from the Dependency Injection page into
  a new `dotnet new web` project on .NET 10
- **THEN** it builds without errors or warnings

### Requirement: Accessible, theme-aware diagrams
Every diagram on the site SHALL have a text alternative that describes what it shows. It SHALL stay
legible in both the light and dark colour schemes, including after a reader switches scheme with the
site's theme control. A diagram without a text alternative SHALL fail the site test gate.

#### Scenario: Screen-reader user reaches the pipeline diagram
- **WHEN** a screen reader reaches the request-pipeline diagram on the Middleware page
- **THEN** it announces a description of the pipeline order and of the request and response flow

#### Scenario: Reader switches to light mode
- **WHEN** a reader switches the site from dark to light with the theme control
- **THEN** the diagram's lines, boxes and labels switch to light-scheme colours and remain legible

#### Scenario: Diagram without a text alternative is caught
- **WHEN** a page includes a diagram with no accessible name
- **THEN** the site test gate fails and names the page

### Requirement: Getting started page orients a .NET 3.5 engineer
The .NET 10 and `dotnet` CLI page SHALL briefly explain how modern .NET relates to .NET Framework:
cross-platform rather than Windows-only, the yearly release cadence with LTS and STS releases, and
SDK-style project files in place of the old ones. It SHALL also explain the difference between an
SDK, a runtime and a target framework.

#### Scenario: Reader asks which .NET to target
- **WHEN** a reader coming from .NET 3.5 reads the page
- **THEN** it says what LTS and STS mean, and why `net10.0` is the target framework used throughout
  the primer

### Requirement: Getting started page teaches dotnetup for installation
The page SHALL teach `dotnetup` as the way to install and manage .NET SDKs and runtimes on a
development machine:
- first-time setup;
- installing by channel or exact version;
- installing the SDK a repository's `global.json` asks for;
- listing, updating and uninstalling.

It SHALL say that `dotnetup` is in public preview, that production servers and container images get
.NET in other ways, and link to Microsoft's other install methods.

#### Scenario: Reader sets up a new machine
- **WHEN** a reader with no .NET installed follows the page
- **THEN** they can install a .NET 10 SDK with `dotnetup` and confirm it with `dotnet --version`

#### Scenario: Reader sees the preview caution
- **WHEN** a reader reads the install section
- **THEN** it states that `dotnetup` is in public preview, and links to the other installation
  methods on Microsoft Learn

### Requirement: Getting started page covers projects and the CLI
The page SHALL explain:
- `global.json` SDK pinning and roll-forward;
- the SDK-style project file (`TargetFramework`, implicit usings, nullable reference types,
  `PackageReference`);
- shared build settings and central package management;
- the everyday `dotnet` CLI commands for creating, building, running, testing, adding packages and
  references, and publishing, with solution files.

#### Scenario: Reader opens a primer sample project
- **WHEN** a reader compares a `.csproj` under `samples/` with the page
- **THEN** every element in it is explained on the page, or by the shared build settings the page
  describes

### Requirement: Getting started page ends with a setup check
The page SHALL end with a recipe that confirms the reader's setup. The recipe SHALL clone the
repository, install the SDK the samples pin, and run exercise 01's solution tests. Only a short
pointer to the next Getting started page, Docker, MAY follow the recipe.

#### Scenario: Reader checks their setup
- **WHEN** a reader follows the setup-check recipe on a machine set up as the page describes
- **THEN** exercise 01's solution tests pass

#### Scenario: Reader moves on after the setup check
- **WHEN** a reader finishes the setup-check recipe
- **THEN** the only section after it is a short pointer to the Docker page, saying that exercises
  01–06 don't need Docker

### Requirement: Docker page explains how to get Docker
The site SHALL have a Docker page that follows the topic page structure. It SHALL say which primer
pages need Docker and that exercises 01–06 don't, and SHALL give a recipe for each of Windows, macOS
and Linux. Each recipe SHALL be a path whose software is free to use at work, or SHALL say which
licence the software needs. The .NET 10 and `dotnet` CLI page SHALL link to it.

#### Scenario: Reader checks whether they need Docker yet
- **WHEN** a reader starting the primer reads the Docker page
- **THEN** it names the pages that use Docker, and says that exercises 01–06 run without it

#### Scenario: Reader at a large company picks a tool
- **WHEN** a reader whose employer has no Docker Desktop or OrbStack subscription reads the recipe
  for their platform
- **THEN** it gives them a path whose software is free to use at work, and says which of the
  alternatives need a paid licence

### Requirement: Windows and macOS readers get Rancher Desktop by default
The Windows and macOS recipes SHALL install Rancher Desktop with its dockerd (moby) engine and with
Kubernetes turned off, so `docker` works from the reader's usual terminal. They SHALL say how
Testcontainers finds the engine. Each SHALL name its alternatives with their licence terms: Docker
Desktop on both, and OrbStack and Colima on macOS.

#### Scenario: Windows reader uses docker from PowerShell
- **WHEN** a Windows reader who followed the recipe opens a new PowerShell
- **THEN** `docker run --rm hello-world` works there, and `DOCKER_HOST` names Rancher Desktop's
  `npipe:////./pipe/docker_engine` for Windows-side tools such as Testcontainers

#### Scenario: Reader runs the dashboard
- **WHEN** a Windows or macOS reader who followed the recipe runs the Logging page's dashboard
  `docker run` command
- **THEN** the dashboard opens in their browser at `http://localhost:18888`, and an app run with
  `dotnet run` sends it telemetry on `localhost:4317`

#### Scenario: Reader picked the wrong engine
- **WHEN** a reader chose containerd at Rancher Desktop's first run, and `docker` isn't found
- **THEN** the page's Pitfalls section says to switch to dockerd (moby) in Rancher Desktop's
  preferences

### Requirement: Advanced Windows path without a desktop app
The Windows recipe SHALL offer, collapsed and labelled advanced, Docker Engine inside a WSL 2 distro
that starts automatically and that Windows-side tools reach through `DOCKER_HOST` over TCP. The TCP
endpoint SHALL listen on loopback only, and the page SHALL say that it has no authentication.

#### Scenario: Advanced reader's Windows tools reach the engine
- **WHEN** a Windows reader who followed the advanced steps opens a new PowerShell
- **THEN** `DOCKER_HOST` points at `tcp://localhost:2375`, and the page's check for that path shows
  that the engine in WSL answers on it

#### Scenario: Engine starts with WSL
- **WHEN** a reader on the advanced path restarts WSL (`wsl --shutdown`) and opens their distro again
- **THEN** the Docker daemon is running without a manual start

### Requirement: Untested steps say so and invite reports
Where the page's steps have not been run end to end on a platform, the page SHALL say so near the
top of that platform's part, and link a new GitHub issue prefilled with a title, the `content`
label, and prompts for the reader's OS and tool versions, the failed step and its output.

#### Scenario: Reader whose step fails
- **WHEN** a step of the Windows or macOS recipe fails for a reader
- **THEN** a note near the top of that part says the steps haven't been tested end to end there, and
  its link opens the prefilled issue

### Requirement: The Docker page ends its setup with a check
The Docker page SHALL end its setup steps with a check that a container runs and that the daemon
answers, with the output to expect. The .NET 10 and `dotnet` CLI page's "Check your setup" recipe
SHALL stay unchanged, because exercises 01–06 don't need Docker.

#### Scenario: Reader checks their Docker setup
- **WHEN** a reader runs the page's check after following their platform's recipe
- **THEN** `docker run --rm hello-world` prints Docker's "Hello from Docker!" message, and
  `docker info --format '{{.ServerVersion}}'` prints a version number

#### Scenario: Reader whose daemon isn't running
- **WHEN** the check fails because the daemon isn't running, or because the user isn't yet in the
  `docker` group
- **THEN** the Docker page's Pitfalls section names that error and how to fix it

### Requirement: dotnet CLI cheatsheet coverage
The `dotnet` CLI cheatsheet SHALL group the everyday commands in copyable blocks, covering:
- installing and listing SDKs and runtimes with `dotnetup`;
- SDK selection and `global.json`;
- creating projects and solutions;
- building, running, watching and testing;
- packages and references;
- tools;
- user secrets;
- EF Core migrations;
- formatting;
- publishing.

#### Scenario: Engineer needs a command quickly
- **WHEN** an engineer opens the cheatsheet to add a NuGet package or run EF Core migrations
- **THEN** the command is on the page in a copyable block, with no "Coming soon" note

### Requirement: Observability pages share one local setup
The Logging page SHALL explain which question each of logs, traces and metrics answers. It SHALL
show one OpenTelemetry setup that exports all three over OTLP to a local dashboard the reader can run
with a single container command. The Tracing and Metrics pages SHALL build on that setup, not repeat
a different one.

#### Scenario: Reader sees their own telemetry
- **WHEN** a reader adds the page's setup to an ASP.NET Core app, starts the dashboard container, and
  sends the app a request
- **THEN** the request's logs, trace and metrics appear in the dashboard

#### Scenario: Reader picks a signal
- **WHEN** a reader wants to know whether to log, trace or count something
- **THEN** the Logging page says which question each signal answers, with an example of each

### Requirement: Observability pages explain production export
Each Observability page SHALL explain that production telemetry leaves the app over OTLP, usually to
an OpenTelemetry Collector, so the backend can change without code changes. Each page SHALL briefly
say how the same telemetry reaches AWS CloudWatch and X-Ray through the AWS Distro for
OpenTelemetry, and link to the Orchestration & AWS page for deployment.

#### Scenario: Reader deploys to AWS
- **WHEN** a reader whose services run on AWS reads any Observability page
- **THEN** it says how the telemetry reaches CloudWatch and X-Ray without changing the app's
  instrumentation code

### Requirement: Logging page gives logging guidance
The Logging page SHALL give practical guidance on:
- what to log, and when;
- choosing a level, with criteria for each one;
- never logging secrets or personal data;
- logging a failure once, where it's handled;
- the cost of logging in hot paths.

#### Scenario: Reader chooses a level
- **WHEN** a reader is unsure whether an event is Information, Warning or Error
- **THEN** the page gives a criterion for each level and an example event at each one

#### Scenario: Reader catches and rethrows
- **WHEN** a reader logs an exception and then rethrows it
- **THEN** the page explains that the failure gets logged more than once, and says where to log it
  instead

### Requirement: Logging page covers the logging API
The Logging page SHALL explain:
- `ILogger<T>`;
- message templates with named, structured properties, and why string interpolation loses them;
- level filtering through configuration;
- log scopes;
- the console and JSON output formats;
- source-generated logging methods for hot paths;
- how log entries carry the current trace and span IDs.

#### Scenario: Reader interpolates a log message
- **WHEN** a reader writes a log call with an interpolated string
- **THEN** the page shows that the values are no longer separate, searchable properties, and shows
  the message-template form

### Requirement: Tracing page gives tracing guidance
The Tracing page SHALL give practical guidance on:
- where to start a span, and where not to;
- naming spans;
- choosing attributes from the OpenTelemetry semantic conventions;
- recording a failure on a span;
- events and links;
- sampling;
- the risks of baggage.

#### Scenario: Reader instruments a method
- **WHEN** a reader wonders whether every method needs its own span
- **THEN** the page says which units of work deserve a span, such as an outbound call, a queue
  message or a meaningful step, and which don't

#### Scenario: Reader records an error
- **WHEN** an operation inside a span fails
- **THEN** the page shows how to set the span's status to error and record the exception on it

### Requirement: Tracing page covers the tracing API
The Tracing page SHALL explain:
- traces, spans and W3C trace context propagation;
- how .NET's activity source and activity map to OpenTelemetry's tracer and span;
- automatic instrumentation for incoming ASP.NET Core requests and outgoing HTTP calls;
- creating custom spans with attributes;
- registering a custom source with OpenTelemetry;
- viewing a trace in the local dashboard.

#### Scenario: Reader's custom spans are missing
- **WHEN** a reader creates spans from their own source but doesn't see them in the dashboard
- **THEN** the page explains that the source must be registered with OpenTelemetry, and that
  starting a span returns nothing when no listener is registered

### Requirement: Metrics page gives metrics guidance
The Metrics page SHALL give practical guidance on:
- when a metric is the right signal, using RED and USE as starting points;
- choosing an instrument for a measurement;
- naming instruments, and units, following the OpenTelemetry semantic conventions;
- keeping tag cardinality low.

#### Scenario: Reader tags a metric with an ID
- **WHEN** a reader adds a user ID or order ID as a metric tag
- **THEN** the page explains why unbounded tag values multiply time series, and what to use instead

#### Scenario: Reader chooses an instrument
- **WHEN** a reader needs to record request durations, a queue length, or a count of orders
- **THEN** the page says which instrument fits each one, and why

### Requirement: Metrics page covers the metrics API
The Metrics page SHALL explain:
- creating meters through dependency injection;
- the counter, up-down counter, histogram and gauge instruments, and their observable forms;
- recording with tags;
- the built-in metrics for ASP.NET Core, Kestrel, outgoing HTTP calls and the .NET runtime;
- watching metrics live with `dotnet-counters`;
- exporting them to the local dashboard.

#### Scenario: Reader watches live metrics
- **WHEN** a reader runs the page's `dotnet-counters` command against a running app
- **THEN** they see its request and runtime metrics update, without installing a global tool

### Requirement: EF Core page covers DbContext and its lifetime
The EF Core page SHALL teach EF Core against SQL Server. It SHALL cover:
- registering a `DbContext` with `AddDbContextPool`, recommended for performance and scalability,
  and why a context is still one per request, never shared across threads;
- pooling's rule that a pooled context holds no per-request state;
- `AddPooledDbContextFactory` for code that outlives a request.

#### Scenario: Reader registers a context
- **WHEN** a reader follows the page's registration example in an ASP.NET Core app
- **THEN** the context reads its connection string from configuration, comes from a pool, and each
  request gets its own context instance

### Requirement: EF Core page covers modelling
The EF Core page SHALL cover modelling by convention, then with the fluent API: keys, required and
maximum-length strings, `decimal` precision, a one-to-many relationship, and an enum value
conversion.

#### Scenario: Reader configures what conventions don't know
- **WHEN** a reader follows the page's fluent configuration for orders
- **THEN** the customer email has a maximum length, the total is `decimal(10,2)`, and the status is
  stored by name

### Requirement: EF Core page covers migrations
The EF Core page SHALL cover creating and applying migrations with the `dotnet ef` tool, and
reviewing what a migration generates. For production, it SHALL cover applying migrations with an
idempotent SQL script or a migration bundle, rather than from the app at startup.

#### Scenario: Reader changes the model
- **WHEN** a reader adds a property to an entity and follows the page's migration steps
- **THEN** a new migration is created, and applying it updates the database schema

#### Scenario: Reader plans a production rollout
- **WHEN** a reader looks for how to apply migrations in production
- **THEN** the page gives the idempotent-script and bundle commands, and says why migrating at app
  startup is risky with several instances

### Requirement: EF Core page covers querying
The EF Core page SHALL show the SQL that its LINQ examples produce, and SHALL cover:
- `ToQueryString()` and SQL logging;
- `Include` against projection with `Select`;
- `AsNoTracking` for read-only queries;
- the N+1 problem, and how to see it in the logged SQL;
- the translation error EF Core raises for a query it can't run in SQL;
- async query methods.

#### Scenario: Reader spots an N+1 query
- **WHEN** a reader compares the page's N+1 example with its fixed version
- **THEN** the page shows the logged SQL for both: one command per row against a single command

### Requirement: EF Core page covers change tracking
The EF Core page SHALL cover:
- entity states, and `SaveChanges` as one unit of work;
- `ExecuteUpdate` and `ExecuteDelete` for set-based changes;
- optimistic concurrency with a `rowversion` token, and the exception a conflict raises.

#### Scenario: Reader saves several changes at once
- **WHEN** a reader adds an entity and modifies another, then calls `SaveChanges` once
- **THEN** the page shows that both changes are saved in one transaction, or neither is

#### Scenario: Reader resolves a concurrency conflict
- **WHEN** two requests save the same row and the page's concurrency token is configured
- **THEN** the page shows that the second save throws `DbUpdateConcurrencyException`, and how to
  handle it

### Requirement: EF Core page maps Entity Framework 6 habits
The EF Core page SHALL have a "Coming from Entity Framework 6" section, after its concepts and before
its Pitfalls. It SHALL map these EF6 habits to EF Core:
- the EDMX designer;
- lazy loading on by default;
- `ObjectContext` or a hand-made context;
- EF6 migrations;
- the `NotSupportedException` for LINQ that EF6 can't translate;
- the `System.Data.Entity` namespace.

#### Scenario: Engineer coming from EF6 wonders where lazy loading went
- **WHEN** an engineer used to EF6's `virtual` navigation properties reads the mapping
- **THEN** it says that lazy loading is off by default in EF Core, and to use `Include` or a
  projection instead

#### Scenario: Engineer coming from EF6 looks for the designer
- **WHEN** an engineer who used an EDMX model reads the mapping
- **THEN** it says that EF Core has no designer, and that the model is C# code kept in step with the
  database through migrations

### Requirement: Docker page covers x64-only images on Apple silicon
The Docker page's macOS part SHALL say that some images the primer uses, such as SQL Server, are
published for x64 only, and how to run them on Apple silicon with the default tool, Rancher Desktop
(the VZ virtual machine type with Rosetta). Its list of pages that need Docker SHALL include the EF
Core page.

#### Scenario: Apple silicon reader starts SQL Server
- **WHEN** a reader on Apple silicon who followed the macOS steps and this note runs exercise 07's
  tests
- **THEN** the SQL Server container starts under emulation, and the tests run

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

### Requirement: Testing page covers running and writing tests
The Testing page SHALL cover running tests with xUnit v3 on Microsoft.Testing.Platform, including
the `global.json` setting the samples use, `dotnet test`, and running a subset of tests. It SHALL
cover writing facts and theories, async tests that use the test's cancellation token, and how
xUnit creates and shares test class instances and fixtures.

#### Scenario: Reader runs one test class
- **WHEN** a reader wants to run only the tests in one class
- **THEN** the page gives a `dotnet test` command that does it on Microsoft.Testing.Platform

#### Scenario: Reader shares an expensive setup
- **WHEN** a reader needs one expensive resource shared by every test in a class, or across classes
- **THEN** the page says each test gets a new class instance, and shows the class fixture and the
  collection fixture

#### Scenario: Reader writes a test that waits
- **WHEN** a reader writes an async test that calls code taking a `CancellationToken`
- **THEN** the page shows passing the test's own cancellation token, so a cancelled run stops it

### Requirement: Testing page covers test doubles
The Testing page SHALL teach replacing a dependency in a test. It SHALL start with hand-written
fakes, then cover Microsoft's fake time provider and fake logger, then NSubstitute for larger
interfaces. It SHALL say when a double is not needed. Moq SHALL get no more than a sentence.

#### Scenario: Reader tests a time rule
- **WHEN** a reader tests code that depends on the current time
- **THEN** the page shows the code taking a `TimeProvider`, and the test moving a fake time provider
  forward, instead of waiting or reading the system clock

#### Scenario: Reader checks a call was made
- **WHEN** a reader needs to check that the code under test called a dependency with certain
  arguments
- **THEN** the page shows it with a hand-written fake, and with NSubstitute

#### Scenario: Reader checks what was logged
- **WHEN** a reader needs to check a log entry the code wrote
- **THEN** the page shows the fake logger collecting it

### Requirement: Testing page covers integration tests and real dependencies
The Testing page SHALL cover testing an ASP.NET Core app in process with `WebApplicationFactory`,
including replacing one of the app's services for the test. It SHALL introduce Testcontainers for
tests that need a real database or broker, and link to exercise 07 for practice.

#### Scenario: Reader calls an endpoint in a test
- **WHEN** a reader wants to send an HTTP request to their app from a test
- **THEN** the page shows a factory for the app's `Program`, a client from it, and an assertion on the
  response, with no port or running server

#### Scenario: Reader replaces a service
- **WHEN** a reader's integration test must not call a real external service
- **THEN** the page shows replacing that service's registration for the test only

#### Scenario: Reader needs a real database
- **WHEN** a reader's tests need a real database
- **THEN** the page shows starting one with Testcontainers, says it needs Docker, and links to the
  EF Core exercise

### Requirement: Testing page covers code coverage
The Testing page SHALL show how to collect code coverage with Microsoft.Testing.Platform, and SHALL
say what a coverage number does not show.

#### Scenario: Reader collects coverage
- **WHEN** a reader follows the page to collect coverage for a test project
- **THEN** they get a coverage report file from `dotnet test`

#### Scenario: Reader reads a high coverage number
- **WHEN** a reader sees high coverage
- **THEN** the page has said that covered code can still be untested, because a line can run without
  any assertion checking what it did

### Requirement: Testing page maps .NET 3.5 habits
The Testing page SHALL have a short table mapping test habits from the .NET 3.5 era (MSTest v1 and
NUnit 2) to their modern xUnit v3 equivalents.

#### Scenario: Reader looks for an expected-exception attribute
- **WHEN** a reader looks for `[ExpectedException]`
- **THEN** the table maps it to asserting on the exception from the call that throws it

#### Scenario: Reader looks for per-test setup
- **WHEN** a reader looks for `[TestInitialize]` or `[SetUp]`
- **THEN** the table maps it to the test class constructor, and teardown to `Dispose` or
  `DisposeAsync`

### Requirement: Security page covers authentication
The Security & auth page SHALL cover authentication in ASP.NET Core:
- claims, the signed-in user, and authentication schemes;
- the difference between a 401 and a 403;
- validating JWT bearer access tokens from an identity provider;
- creating development tokens with `dotnet user-jwts`;
- signing users in to a server-rendered app with OpenID Connect.

#### Scenario: Reader secures an API with tokens
- **WHEN** a reader follows the page's JWT bearer example for an API
- **THEN** the page names the settings that are validated (issuer, audience, signing keys and
  lifetime), and the API answers a request without a valid token with 401

#### Scenario: Reader calls an API without an identity provider
- **WHEN** a reader wants a token for local development
- **THEN** the page shows `dotnet user-jwts` creating one, with chosen scopes and roles, that the
  API accepts

#### Scenario: Reader signs users in to a web app
- **WHEN** a reader follows the page's OpenID Connect example
- **THEN** an unauthenticated user is redirected to the identity provider, and the app keeps the
  signed-in user in a cookie

### Requirement: Security page covers authorization
The Security & auth page SHALL cover authorization:
- requiring an authenticated user on endpoints, and opting endpoints out;
- policies built from claims, scopes and roles;
- custom requirements with their handlers;
- resource-based checks, which need the resource first;
- a fallback policy, so endpoints are private unless marked otherwise.

#### Scenario: Reader requires a scope
- **WHEN** a reader adds the page's scope policy to an endpoint
- **THEN** a signed-in caller whose token lacks the scope gets 403

#### Scenario: Reader limits users to their own data
- **WHEN** a reader follows the page's resource-based example
- **THEN** a user can read a resource they own, and gets 403 for one they don't

#### Scenario: Reader adds an endpoint and forgets to protect it
- **WHEN** an app has the page's fallback policy and a new endpoint has no authorization metadata
- **THEN** the endpoint still requires an authenticated user

### Requirement: Security page covers secrets and data protection
The Security & auth page SHALL cover keeping secrets out of source control: user secrets in
development, and a secret store in production read through configuration. It SHALL name AWS Secrets
Manager and Parameter Store as stores and leave their setup to the AWS area. It SHALL cover what data
protection protects, and why its keys must persist and be shared.

#### Scenario: Reader stores a development secret
- **WHEN** a reader follows the page's user-secrets example
- **THEN** the app reads the value through configuration, and the value is not in the repository

#### Scenario: Reader runs several instances
- **WHEN** a reader deploys an app that uses cookies or antiforgery to more than one instance, or
  restarts it
- **THEN** the page says to persist the data protection keys in shared storage, and what breaks if
  they don't

### Requirement: Security page covers HTTPS, rate limiting and antiforgery
The Security & auth page SHALL cover HTTPS redirection and HSTS, and the development certificate. It
SHALL cover the built-in rate limiter, with limits per user or per client and the status a rejected
request gets. It SHALL cover antiforgery: when cookie authentication needs it, and why an API that
takes bearer tokens doesn't.

#### Scenario: Reader turns on HSTS
- **WHEN** a reader follows the page's HTTPS example
- **THEN** the page says that HSTS is sent outside Development only, and why

#### Scenario: Caller exceeds a rate limit
- **WHEN** a caller sends more requests than the page's limiter allows in its window
- **THEN** the extra requests get 429, and the page notes that the limiter's default status is
  different unless it's set

#### Scenario: Reader posts a form with cookie authentication
- **WHEN** a reader's server-rendered form posts with the user's auth cookie
- **THEN** the page shows the antiforgery token that the post needs, and a post without it is
  rejected

### Requirement: Security page maps OWASP risks and security headers
The Security & auth page SHALL have a short table mapping the OWASP Top 10 risk categories to the
ASP.NET Core defences that address them, with links to the page's own sections where it covers them.
It SHALL name the response headers worth adding to an app, and what each one does.

#### Scenario: Reader checks an OWASP category
- **WHEN** a reader looks up broken access control in the table
- **THEN** it points to the page's authorization section and the fallback policy

#### Scenario: Reader adds security headers
- **WHEN** a reader follows the page's headers example
- **THEN** the app's responses carry each header the page names, with the value shown

### Requirement: Security page maps .NET 3.5 habits
The Security & auth page SHALL have a "Coming from .NET 3.5" section, after its concepts and before
its Pitfalls. It SHALL map these habits: Forms authentication, Windows authentication, `web.config`
authorization rules, membership and role providers, `<machineKey>`, and secrets in `web.config`.

#### Scenario: Engineer looks for Forms authentication
- **WHEN** an engineer used to Forms authentication reads the mapping
- **THEN** it points to cookie authentication with OpenID Connect against an identity provider

#### Scenario: Engineer looks for machineKey
- **WHEN** an engineer used to setting `<machineKey>` in `web.config` reads the mapping
- **THEN** it says that data protection replaces it, and links to the page's data protection section

### Requirement: Async page covers async end to end and cancellation
The Async & performance page SHALL cover what `await` does while it waits, keeping code async from
the endpoint down to the I/O, and cancellation: taking the request's token, passing it to every call
that waits, timeouts, and what a cancelled call throws. It SHALL show independent awaits run
concurrently, and when `ValueTask` is worth using, with the rule that it is awaited only once.

#### Scenario: Reader passes cancellation down
- **WHEN** a reader follows the page's cancellation example and the client disconnects mid-request
- **THEN** the work the endpoint started stops, and the page shows what is thrown

#### Scenario: Reader runs independent calls together
- **WHEN** a reader follows the page's example of two independent awaits
- **THEN** the page shows them taking the time of the slower one, not the sum, with the measured
  times

### Requirement: Async page covers blocking and thread-pool starvation
The Async & performance page SHALL explain why blocking on a task (`.Result`, `.Wait()`,
`GetAwaiter().GetResult()`) holds a thread, how many such requests starve the thread pool, and what
`async void` does to an exception. It SHALL show how to see starvation in a running app with
`dotnet-counters`, and how to find the blocking call with `dotnet-trace`.

#### Scenario: Reader sees starvation in the counters
- **WHEN** a reader runs the page's blocking example under load and watches it with
  `dotnet-counters`
- **THEN** the page shows the thread-pool counters it reports, with the real values, against the
  same load on the async version

#### Scenario: Reader learns what async void does
- **WHEN** a reader reads the page's `async void` section
- **THEN** it says what happens to an exception thrown inside one, and what to write instead

### Requirement: Async page covers spans, allocations and pooling
The Async & performance page SHALL cover `Span<T>` and `Memory<T>`: slicing without copying, and
where each may be stored. It SHALL cover finding allocations, renting buffers from a pool, building
strings without intermediate copies, source-generated JSON serialization, and frozen collections for
lookups built once and read often. Each claim of a saving SHALL come with a measured result.

#### Scenario: Reader parses without allocating
- **WHEN** a reader follows the page's span example
- **THEN** the page shows its allocations against the string-splitting version, as measured

#### Scenario: Reader knows where a span can't go
- **WHEN** a reader wants to keep a buffer across an `await` or in a field
- **THEN** the page says that a span can't, and that `Memory<T>` can

### Requirement: Async page covers caching
The Async & performance page SHALL cover caching data in the app with a cache that also stops many
concurrent requests for a missing entry from all computing it, and caching whole responses with
output caching. It SHALL say how entries expire and how to remove them.

#### Scenario: Reader caches an expensive lookup
- **WHEN** many requests ask for the same uncached entry at once, in the page's example
- **THEN** the page shows that the value is computed once

#### Scenario: Reader caches a response
- **WHEN** a reader adds the page's output caching to an endpoint
- **THEN** a repeated request within the expiry gets the cached response, and the page shows how to
  tell

### Requirement: Async page covers benchmarking
The Async & performance page SHALL cover measuring with BenchmarkDotNet: writing a benchmark,
including allocations, running it in Release, and reading its results. It SHALL say why a
`Stopwatch` loop misleads, and link to the samples' benchmark project.

#### Scenario: Reader reads a benchmark result
- **WHEN** a reader reads the page's benchmark results
- **THEN** the table is real output, and the page explains its mean, error and allocated columns

### Requirement: Async page maps .NET 3.5 habits
The Async & performance page SHALL have a "Coming from .NET 3.5" section, after its concepts and
before its Pitfalls. It SHALL map these habits: the `BeginXxx`/`EndXxx` pattern, the event-based
async pattern, `ThreadPool.QueueUserWorkItem`, `Thread` for background work, `HttpRuntime.Cache`,
and `string` concatenation in loops.

#### Scenario: Engineer looks for Begin and End
- **WHEN** an engineer used to `BeginXxx`/`EndXxx` reads the mapping
- **THEN** it points to the `async` method and `await`, and to the page's async section

#### Scenario: Engineer looks for HttpRuntime.Cache
- **WHEN** an engineer used to `HttpRuntime.Cache` reads the mapping
- **THEN** it points to the page's caching section

### Requirement: Containers page covers images and publishing without a Dockerfile
The Containers page SHALL describe the official .NET images (`sdk`, `aspnet`, `runtime` and
`runtime-deps`), their tags, and which one an app builds on and which it runs on. It SHALL show
building an image with `dotnet publish` and no Dockerfile, and setting the image's base, name, tag,
port and user through the project's container properties.

#### Scenario: Reader publishes an image without a Dockerfile
- **WHEN** a reader follows the page's `dotnet publish` example in an ASP.NET Core project
- **THEN** an image is built into their local Docker, and the page shows the command's real output
  and how to run the image

#### Scenario: Reader picks a base image
- **WHEN** a reader wants to know which image to build on and which to run on
- **THEN** the page says that the build uses `sdk`, that an ASP.NET Core app runs on `aspnet`, and
  when `runtime` or `runtime-deps` fits instead

### Requirement: Containers page covers multi-stage Dockerfiles
The Containers page SHALL show a multi-stage Dockerfile for an ASP.NET Core app, with a build stage
on the SDK image and a runtime stage without it. It SHALL explain layer caching, copying the project
file and restoring before the rest of the source, and keeping build output and secrets out of the
build context with `.dockerignore`.

#### Scenario: Reader builds the page's Dockerfile
- **WHEN** a reader builds the page's Dockerfile and runs the image
- **THEN** the app answers, and the final image holds no SDK

#### Scenario: Reader rebuilds after a code change
- **WHEN** a reader changes a source file and builds again
- **THEN** the page shows that the restore layer comes from the cache, as measured

### Requirement: Containers page covers non-root and smaller images
The Containers page SHALL say that .NET 8 and later images listen on port 8080 and provide a
non-root `app` user, and why an image shouldn't run as root. It SHALL compare the size of the same
app on the default, chiseled, chiseled-extra and Native AOT bases, with measured sizes, and say what
each leaves out and when it matters.

#### Scenario: Reader compares base images
- **WHEN** a reader reads the page's image-size comparison
- **THEN** the sizes are real measurements of one app, with the platform they were measured on, and
  each base has its trade-off stated

#### Scenario: Reader needs a shell in a chiseled container
- **WHEN** a reader wants to look inside a running container that has no shell
- **THEN** the page says why there's none, and how to inspect it anyway

### Requirement: Containers page covers configuration through the environment
The Containers page SHALL show configuring a containerized app with environment variables: the `__`
separator for nested settings, `ASPNETCORE_HTTP_PORTS` and `ASPNETCORE_ENVIRONMENT`, and that
environment variables override `appsettings.json`. It SHALL say that secrets don't belong in an
image, and link to the Security & auth page for where they go instead.

#### Scenario: Reader overrides a nested setting
- **WHEN** a reader runs the page's image with an environment variable for a nested setting
- **THEN** the app uses the variable's value, and the page shows the variable's name

### Requirement: Containers page covers Compose locally
The Containers page SHALL show running an app with the database it uses on a developer's machine,
using Docker Compose: a `compose.yaml` that builds or names the app's image, passes its settings as
environment variables, and starts the app only once the database is ready. It SHALL say how to stop
the services and remove their data.

#### Scenario: Reader starts an app and its database
- **WHEN** a reader runs the page's `compose.yaml` with `docker compose up`
- **THEN** SQL Server starts, the app starts after it and answers, and the page shows how to tell

#### Scenario: Apple silicon reader runs the database
- **WHEN** a reader on Apple silicon runs the page's `compose.yaml`
- **THEN** the page points to the Docker page's note on x64-only images

### Requirement: Containers page covers registries and ECR
The Containers page SHALL show tagging an image and pushing it to a registry, both with `docker`
and with `dotnet publish`, as verified against a local registry. It SHALL show the steps that are
specific to Amazon ECR (signing in, and the repository's address) from the AWS documentation, and
SHALL say that those steps weren't run for the page.

#### Scenario: Reader pushes to a local registry
- **WHEN** a reader follows the page's push example against a local registry container
- **THEN** the image is pushed, and pulling it back by its tag works

#### Scenario: Reader pushes to ECR
- **WHEN** a reader wants to push to Amazon ECR
- **THEN** the page gives the sign-in command and the repository address format, links to the AWS
  documentation they come from, and says they weren't run for the page

### Requirement: Containers page covers image scanning
The Containers page SHALL show scanning an image for packages with known vulnerabilities with
Trivy, run as a container so that nothing is installed. It SHALL show real results for the default
and chiseled images of the same app, and say that an image is patched by rebuilding it on an
updated base image.

#### Scenario: Reader scans an image
- **WHEN** a reader runs the page's Trivy command against an image they built
- **THEN** the page shows the real summary it printed, with the date it was run, since the results
  change as advisories are published

#### Scenario: Reader patches an image
- **WHEN** a scan reports a vulnerable package in the base image
- **THEN** the page says to pull the updated base and rebuild, not to patch the running container

### Requirement: Containers page maps .NET 3.5 habits
The Containers page SHALL have a "Coming from .NET 3.5" section, after its concepts and before its
Pitfalls. It SHALL map these habits: deploying to IIS by copying files or with an installer,
settings in `web.config` and `machine.config`, an app pool running under a privileged account, and
patching the server in place.

#### Scenario: Engineer looks for web.config
- **WHEN** an engineer used to `web.config` transforms reads the mapping
- **THEN** it points to `appsettings.json` and environment variables, and to the page's
  configuration section

#### Scenario: Engineer looks for server patching
- **WHEN** an engineer used to patching a server in place reads the mapping
- **THEN** it points to rebuilding the image on an updated base, and to the page's scanning section

### Requirement: Orchestration page covers health checks
The Orchestration & AWS page SHALL show adding ASP.NET Core health checks, and SHALL explain the
difference between liveness and readiness: liveness reports only whether the process works, while
readiness also checks the dependencies it needs to serve requests. It SHALL say why a liveness check
mustn't check a shared dependency such as the database, and show mapping each endpoint to its own
checks.

#### Scenario: Reader separates liveness from readiness
- **WHEN** a reader follows the page's health-check example and the database becomes unreachable
- **THEN** the page shows, from a real run, that the liveness endpoint still reports healthy and
  the readiness endpoint reports unhealthy

#### Scenario: Reader asks why liveness ignores the database
- **WHEN** a reader wonders why liveness doesn't check the database
- **THEN** the page says that an orchestrator restarts an instance whose liveness check fails, so a
  database outage would restart every instance without fixing anything

### Requirement: Orchestration page covers graceful shutdown
The Orchestration & AWS page SHALL explain how a stop request reaches an ASP.NET Core app in a
container (SIGTERM, then SIGKILL after the stop timeout), that the host stops taking new requests
and waits for in-flight ones up to its shutdown timeout, and how to fail readiness once shutdown
starts. It SHALL show what `docker stop` does to an in-flight request, as measured.

#### Scenario: Reader stops a container with a request in flight
- **WHEN** a reader runs the page's `docker stop` example while a slow request is in flight
- **THEN** the page shows, from a real run, that the request completes before the app exits, and
  what the app logged

#### Scenario: Reader's shutdown takes longer than the timeouts
- **WHEN** a reader's in-flight work takes longer than the host's shutdown timeout or the
  container's stop timeout
- **THEN** the page says which one cuts the work off, and which setting to change

### Requirement: Orchestration page covers .NET Aspire locally
The Orchestration & AWS page SHALL show running an API with SQL Server on a developer's machine
through a .NET Aspire AppHost, created and run with the Aspire CLI. It SHALL cover the dashboard, a
resource reference that passes the connection string to the API, waiting for the database, and
service discovery. It SHALL link to Aspire's documentation at aspire.dev.

#### Scenario: Reader runs the AppHost
- **WHEN** a reader runs the page's AppHost with the Aspire CLI
- **THEN** SQL Server and the API start, the API reaches the database through the connection string
  Aspire gave it, and the page shows the real output and where to find the dashboard

#### Scenario: Reader compares Aspire with Compose
- **WHEN** a reader who has seen the Containers page's Compose example reads the Aspire section
- **THEN** the page says what each is for, and that Aspire's AppHost is for local development, not
  a production deployment

### Requirement: Orchestration page covers ECS
The Orchestration & AWS page SHALL show running a containerized ASP.NET Core app on Amazon ECS with
Fargate: a task definition with the container's image, port, health check, environment and stop
timeout, a task role for AWS credentials, and the load balancer's health check against the
readiness endpoint. It SHALL take these from the AWS documentation, link to it, and say that they
weren't run for the page.

#### Scenario: Reader writes a task definition
- **WHEN** a reader adapts the page's ECS task definition
- **THEN** it names the image, the port, the health check, the environment and the stop timeout,
  and the page links to the AWS documentation for each part

#### Scenario: Reader checks what was run
- **WHEN** a reader wants to know whether the ECS steps were tested
- **THEN** the page says that they weren't run for it, and that their syntax was checked

### Requirement: Orchestration page covers EKS and Lambda
The Orchestration & AWS page SHALL say when EKS or Lambda fits instead of ECS. For EKS, it SHALL
cover the AWS side only (the cluster, the load balancer and pod credentials) and point to
Kubernetes's own documentation for the rest. For Lambda, it SHALL show hosting an ASP.NET Core API
on Lambda, with the C# compiled. It SHALL say that neither was run for the page.

#### Scenario: Reader chooses where to run a service
- **WHEN** a reader wants to know whether to use ECS, EKS or Lambda
- **THEN** the page states what each one suits, and the trade-off of each

#### Scenario: Reader hosts an API on Lambda
- **WHEN** a reader follows the page's Lambda example
- **THEN** the page shows the code that hosts the API on Lambda, which compiles, and says how it's
  deployed, from the AWS documentation

### Requirement: Orchestration page covers configuration from AWS
The Orchestration & AWS page SHALL show reading configuration from Systems Manager Parameter Store
and secrets from Secrets Manager into ASP.NET Core configuration, with the C# compiled. It SHALL say
that the app gets its AWS credentials from its task or pod role, not from keys in its configuration.

#### Scenario: Reader loads settings from Parameter Store
- **WHEN** a reader follows the page's Parameter Store example
- **THEN** the page shows how a parameter's name maps to a configuration key, and that the
  example wasn't run against AWS

#### Scenario: Reader looks for where the access keys go
- **WHEN** a reader wants to know where to put AWS access keys
- **THEN** the page says that a service on ECS, EKS or Lambda gets credentials from its role, and
  that keys don't go in configuration

### Requirement: Orchestration page maps .NET 3.5 habits
The Orchestration & AWS page SHALL have a "Coming from .NET 3.5" section, after its concepts and
before its Pitfalls. It SHALL map these habits: a load balancer pinging a page, IIS recycling the app
pool, settings changed on the server, and a deployment that takes the site offline.

#### Scenario: Engineer looks for the ping page
- **WHEN** an engineer used to a load balancer pinging a page reads the mapping
- **THEN** it points to the readiness endpoint and the page's health-check section

#### Scenario: Engineer looks for app pool recycling
- **WHEN** an engineer used to IIS recycling the app pool reads the mapping
- **THEN** it points to the orchestrator replacing an instance that fails its liveness check, and
  to the page's graceful-shutdown section

### Requirement: C# language pages share a shape
Each C# language page SHALL open with what the reader will be able to do afterwards, explain its
features with code, and end with a "Pitfalls" section. It SHALL tag each feature it introduces with
the C# version that introduced it. The Null safety, Types and Patterns pages SHALL each have a
"Coming from C# 3" table before their Pitfalls, and SHALL end by pointing to Exercise 14.

#### Scenario: Reader wonders how new a feature is
- **WHEN** a reader meets a feature on a C# page, such as list patterns
- **THEN** the page says which C# version introduced it, and the version matches Microsoft Learn's
  "What's new in C#" pages

#### Scenario: Reader maps a C# 3 habit
- **WHEN** a reader who would write a C# 3 class with private fields and a constructor reads the
  Types page
- **THEN** its "Coming from C# 3" table points them to the modern form, and to the section that
  explains it

#### Scenario: Reader looks for practice
- **WHEN** a reader reaches the end of the Null safety, Types or Patterns page
- **THEN** it points to Exercise 14 on the section's first page

### Requirement: C# examples are compiled and measured
Every C# example on the C# language pages and the C# cheatsheet SHALL compile with the primer's
pinned .NET SDK and its warnings-as-errors settings, unless the page shows it as code that doesn't
compile. Every compiler message and program output a page shows SHALL come from a real build or run.

#### Scenario: Reader copies an example
- **WHEN** a reader copies a C# example from a C# page into a project on the primer's SDK
- **THEN** it compiles, apart from any example the page marks as not compiling, which fails with the
  message the page shows

#### Scenario: Reader compares a warning
- **WHEN** a C# page shows a compiler warning, such as a nullable warning
- **THEN** its code and text are the ones the compiler gave for that example

### Requirement: C# index covers modern C# at a glance
The C# language section's first page SHALL show one class written in C# 3 and in modern C#, side by
side, and then the syntax that removes ceremony from a file. It SHALL link to the C# cheatsheet near
the top, point to the Async & performance page for async and `Span<T>`, and describe Exercise 14 after
its Pitfalls.

#### Scenario: Reader compares the two versions
- **WHEN** a reader opens the first C# page
- **THEN** it shows the same class in C# 3 and in modern C#, and names each feature that changed it,
  with a link to the page that covers it

#### Scenario: Reader meets the top of a modern file
- **WHEN** a reader looks for a feature they meet at the top of a file or a method
- **THEN** the page covers file-scoped namespaces, implicit and global usings, top-level statements,
  target-typed `new()`, collection expressions, `params` collections, raw string literals, string
  interpolation, expression-bodied members, using declarations and local functions

#### Scenario: Reader looks for async
- **WHEN** a reader looks for `async` and `await` in the C# section
- **THEN** the first page points them to the Async & performance page

### Requirement: C# null safety page covers nullable reference types
The Null safety page SHALL explain nullable reference types: annotations, the warnings, the
null-handling operators, `is null` and `is not null`, the null-forgiving operator, the nullable
attributes, and null-conditional assignment. It SHALL show how to turn nullable on in an existing
codebase a file at a time, with the warnings that appear, from a real build.

#### Scenario: Reader turns nullable on in old code
- **WHEN** a reader follows the page's steps to turn nullable on in a file of C# 3-style code
- **THEN** the page shows the warnings the build gives, and how each is fixed

#### Scenario: Reader is tempted to silence a warning
- **WHEN** a reader reaches for the null-forgiving operator to make a warning go away
- **THEN** the page says when it's justified and when it hides a bug, and its Pitfalls name that
  mistake

### Requirement: C# types page covers records and constructors
The Types page SHALL explain records and record structs, `init` and `required` members,
`with`-expressions, primary constructors on classes and on records, tuples and deconstruction, the
`field` keyword, and extension members. It SHALL say how a class's primary constructor parameters
differ from a record's.

#### Scenario: Reader compares two records
- **WHEN** a reader compares two records with the same values
- **THEN** the page shows that they're equal, and what their `ToString` prints, from a real run

#### Scenario: Reader uses a primary constructor on a class
- **WHEN** a reader gives a class a primary constructor
- **THEN** the page says that its parameters are captured for the class's members, not exposed as
  properties as a record's are

### Requirement: C# patterns page covers pattern matching
The Patterns page SHALL explain `is` patterns, switch expressions, and property, relational, logical
and list patterns. It SHALL show an if/else chain rewritten as a switch expression, and what the
compiler says when a switch expression doesn't handle every input, from a real build.

#### Scenario: Reader rewrites an if/else chain
- **WHEN** a reader compares the page's if/else chain with its switch expression
- **THEN** both give the same results for every case the page lists

#### Scenario: Reader leaves a case out
- **WHEN** a switch expression on the page doesn't cover every input
- **THEN** the page shows the compiler's warning, and says what happens at run time when no arm
  matches

### Requirement: Topic pages link C# features to the C# section
A topic page that explains a C# feature in passing SHALL link to the C# language page that covers
that feature.

#### Scenario: Reader meets a primary constructor on the DI page
- **WHEN** a reader reads the Dependency Injection page's primary constructor example
- **THEN** it links to the Types page's section on primary constructors

#### Scenario: Reader meets nullable reference types on the CLI page
- **WHEN** a reader reads the CLI page's note on the `Nullable` setting
- **THEN** it links to the Null safety page
