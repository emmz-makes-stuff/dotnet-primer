# topic-page-content Specification

## Purpose

Sets what a learning-path page with full content must contain and guarantee. An engineer moving off
.NET 3.5 should be able to learn a topic from that page alone, and then go to Microsoft Learn for
depth.

## Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. That means the ASP.NET Core entry page, the Configuration, Dependency Injection and
Middleware pages and their three cheatsheets, the .NET 10 and `dotnet` CLI page with the `dotnet`
CLI cheatsheet, and the Logging, Tracing and Metrics pages with their three cheatsheets. If one of
them shows the placeholder, the site test gate SHALL fail.

#### Scenario: Reader opens a finished page
- **WHEN** a reader opens the Dependency Injection page
- **THEN** the page has no "Coming soon" note

#### Scenario: Reader opens the finished dotnet CLI cheatsheet
- **WHEN** a reader opens the `dotnet` CLI cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished Observability page
- **WHEN** a reader opens the Logging, Tracing or Metrics page, or its cheatsheet
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
repository, install the SDK the samples pin, and run exercise 01's solution tests.

#### Scenario: Reader checks their setup
- **WHEN** a reader follows the setup-check recipe on a machine set up as the page describes
- **THEN** exercise 01's solution tests pass

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
