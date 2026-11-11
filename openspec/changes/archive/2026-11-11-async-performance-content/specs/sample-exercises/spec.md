## MODIFIED Requirements

### Requirement: Site describes each exercise
Each exercise SHALL be described on the site page of the subject area it practises. The description
SHALL cover the goal, the steps, and how to run the tests. It SHALL link to the exercise folder in the
`emmz-makes-stuff/dotnet-primer` repository on GitHub.

#### Scenario: Engineer finds the Configuration exercise
- **WHEN** an engineer reads the Configuration area page
- **THEN** it describes exercise 01, with a link to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/01-configuration`

#### Scenario: Engineer finds the Dependency Injection exercise
- **WHEN** an engineer reads the Dependency Injection area page
- **THEN** it describes exercise 02, with a link to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/02-dependency-injection`

#### Scenario: Engineer finds the Middleware exercise
- **WHEN** an engineer reads the Middleware area page
- **THEN** it describes exercise 03, with a link to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/03-middleware`

#### Scenario: Engineer finds the Observability exercises
- **WHEN** an engineer reads the Logging, Tracing or Metrics page
- **THEN** it describes exercise 04, 05 or 06 respectively, with a link to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/04-logging`,
  `…/05-tracing` or `…/06-metrics`

#### Scenario: Engineer finds the EF Core exercise
- **WHEN** an engineer reads the EF Core page
- **THEN** it describes exercise 07, says that its tests need Docker and links to the Docker page,
  and links to `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/07-ef-core`

#### Scenario: Engineer finds the gRPC exercise
- **WHEN** an engineer reads the gRPC page
- **THEN** it describes exercise 08, says that it needs no Docker, and links to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/08-grpc`

#### Scenario: Engineer finds the Testing exercise
- **WHEN** an engineer reads the Testing page
- **THEN** it describes exercise 09, says that it needs no Docker and that the engineer writes the
  tests, shows how to run the checker, and links to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/09-testing`

#### Scenario: Engineer finds the Security exercise
- **WHEN** an engineer reads the Security & auth page
- **THEN** it describes exercise 10, says that it needs no Docker and no identity provider, and
  links to `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/10-security`

#### Scenario: Engineer finds the Async exercise
- **WHEN** an engineer reads the Async & performance page
- **THEN** it describes exercise 11, says that it needs no Docker, and links to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/11-async`

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: Async exercise fixes blocking and cancellation
Exercise 11 SHALL give the engineer an orders API, in memory with no database or Docker, that
calls a slow dependency. The engineer SHALL make the API stop blocking threads while it waits, and
SHALL make it stop the dependency's work when the caller cancels or the request is aborted.

#### Scenario: Requests don't block threads while they wait
- **WHEN** the tests send concurrent requests to an endpoint that waits on the slow dependency
- **THEN** no request holds a thread while it waits, and a version that blocks a thread fails the
  test with a message that says so

#### Scenario: Cancellation reaches the dependency
- **WHEN** the tests cancel a request while the dependency is still working
- **THEN** the dependency observes the cancellation, and stops before it finishes

#### Scenario: Starter blocks threads
- **WHEN** the tests run against the starter
- **THEN** the blocking and cancellation tests fail, each with a message that names what's wrong
  and the TODO that fixes it

### Requirement: Async exercise fixes lost failures and sequential awaits
In exercise 11, the engineer SHALL make a background step's failure observable instead of lost,
and SHALL make independent calls to the dependency run concurrently instead of one after another.

#### Scenario: A failed background step is reported
- **WHEN** a request starts work that fails after the endpoint has answered
- **THEN** the failure is recorded where the tests can see it, and doesn't crash the process

#### Scenario: Independent calls overlap
- **WHEN** an endpoint needs several independent results from the slow dependency
- **THEN** it takes about as long as the slowest call, not the sum of all of them

### Requirement: Benchmark project builds but isn't run by the gates
The samples SHALL include a benchmark project that measures the Async & performance page's pooling
and fast-API examples, with time and allocations per operation. The samples build gate SHALL
compile it with warnings treated as errors. No gate SHALL run it.

#### Scenario: Benchmarks compile with the samples
- **WHEN** the samples build gate runs
- **THEN** the benchmark project compiles, and a compile error in it fails the gate

#### Scenario: The test gate leaves the benchmarks alone
- **WHEN** the samples test gate runs, locally or in CI
- **THEN** it runs no benchmarks

#### Scenario: Engineer runs the benchmarks
- **WHEN** an engineer runs the benchmark project in Release, as the samples README says
- **THEN** it reports the mean time and the bytes allocated for each benchmark
