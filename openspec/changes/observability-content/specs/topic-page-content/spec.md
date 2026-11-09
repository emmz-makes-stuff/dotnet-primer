# Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

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
