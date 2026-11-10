## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. If one of them shows the placeholder, the site test gate SHALL fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection and Middleware pages and their three cheatsheets, the .NET 10 and `dotnet`
  CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages with their three
  cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its cheatsheet, and the
  Testing page with its cheatsheet

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

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

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

### Requirement: Testing page covers snapshot testing
The Testing page SHALL show snapshot testing with Verify: the first run creating the snapshot to
approve, later runs comparing against it, and what to do when a change is intended.

#### Scenario: Reader asserts on a large object
- **WHEN** a reader wants to check a large response object without writing an assertion per property
- **THEN** the page shows verifying it against an approved snapshot file, kept in source control

#### Scenario: Snapshot differs
- **WHEN** the object changes and no longer matches its snapshot
- **THEN** the page says the test fails with the difference, and how to accept the new snapshot when
  the change is intended

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
