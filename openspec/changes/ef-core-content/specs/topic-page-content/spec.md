## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. That means the ASP.NET Core entry page, the Configuration, Dependency Injection and
Middleware pages and their three cheatsheets, the .NET 10 and `dotnet` CLI page with the `dotnet`
CLI cheatsheet, the Logging, Tracing and Metrics pages with their three cheatsheets, and the EF Core
page with its cheatsheet. If one of them shows the placeholder, the site test gate SHALL fail.

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

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

### Requirement: EF Core page covers DbContext and modelling
The EF Core page SHALL teach EF Core against SQL Server. It SHALL cover:
- registering a `DbContext` with `AddDbContext`, and why its scoped lifetime means one context per
  request, never shared across threads;
- `AddDbContextFactory` or context pooling for code that outlives a request;
- modelling by convention, then with the fluent API: keys, required and maximum-length strings,
  `decimal` precision, a one-to-many relationship, and an enum value conversion.

#### Scenario: Reader registers a context
- **WHEN** a reader follows the page's registration example in an ASP.NET Core app
- **THEN** the context reads its connection string from configuration, and each request gets its own
  context instance

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
