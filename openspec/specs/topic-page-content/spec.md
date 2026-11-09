# topic-page-content Specification

## Purpose

Sets what a learning-path page with full content must contain and guarantee. An engineer moving off
.NET 3.5 should be able to learn a topic from that page alone, and then go to Microsoft Learn for
depth.

## Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. In this change that means the ASP.NET Core entry page, the Configuration,
Dependency Injection and Middleware pages, and their three cheatsheets. If one of them shows the
placeholder, the site test gate SHALL fail.

#### Scenario: Reader opens a finished page
- **WHEN** a reader opens the Dependency Injection page
- **THEN** the page has no "Coming soon" note

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
