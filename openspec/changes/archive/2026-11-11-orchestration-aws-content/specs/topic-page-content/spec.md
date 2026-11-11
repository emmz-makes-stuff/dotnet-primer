## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. If one of them shows the placeholder, the site test gate SHALL fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection and Middleware pages and their three cheatsheets, the .NET 10 and `dotnet`
  CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages with their three
  cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its cheatsheet, the Testing
  page with its cheatsheet, the Security & auth page with its cheatsheet, the Async & performance
  page with its cheatsheet, the Containers page with its cheatsheet, and the Orchestration & AWS
  page with its cheatsheet

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

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

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
