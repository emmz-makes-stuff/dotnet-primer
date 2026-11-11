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

#### Scenario: Engineer finds the Containers exercise
- **WHEN** an engineer reads the Containers page
- **THEN** it describes exercise 12, says that its tests need Docker and links to the Docker page,
  and links to `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/12-containers`

#### Scenario: Engineer finds the health and shutdown exercise
- **WHEN** an engineer reads the Orchestration & AWS page
- **THEN** it describes exercise 13, says that it needs no Docker, and links to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/13-health-and-shutdown`

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: Health exercise separates liveness from readiness
Exercise 13 SHALL give the engineer an orders API, with a store the tests can take offline, whose
health checks are wrong: its liveness endpoint checks the store, and it has no readiness endpoint.
The engineer SHALL make liveness report only on the process, and add a readiness endpoint that
checks the store and reports unhealthy once the app starts shutting down.

#### Scenario: Store goes offline
- **WHEN** the tests take the store offline
- **THEN** the liveness endpoint still reports healthy, and the readiness endpoint reports unhealthy

#### Scenario: App starts shutting down
- **WHEN** the app has been told to stop and is still finishing in-flight work
- **THEN** the readiness endpoint reports unhealthy

#### Scenario: Starter's health checks are wrong
- **WHEN** the tests run against the starter
- **THEN** the health tests fail, each with a message that names what's wrong and the TODO that
  fixes it

### Requirement: Health exercise shuts down gracefully
Exercise 13 SHALL have an order that takes a few seconds to place. The engineer SHALL make the app
finish an order that is in flight when it's told to stop, instead of cutting it off, without
changing how long an order takes.

#### Scenario: Stop arrives during an order
- **WHEN** the tests tell the app to stop while an order is being placed
- **THEN** the order completes and is stored, and the caller gets a success response

#### Scenario: Starter drops the order
- **WHEN** the tests run against the starter
- **THEN** the shutdown test fails with a message that names what's wrong and the TODO that fixes
  it

#### Scenario: Exercise needs no Docker
- **WHEN** an engineer runs exercise 13's tests on a machine without Docker
- **THEN** they run, because the store is in memory and the app runs in the test process
