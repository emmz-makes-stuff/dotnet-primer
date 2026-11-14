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

#### Scenario: Engineer finds the modern C# exercise
- **WHEN** an engineer reads the C# language section's first page
- **THEN** it describes exercise 14, says that it needs no Docker, links each step to the C# page
  it practises, and links to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/14-modern-csharp`

#### Scenario: Engineer finds the session and state exercise
- **WHEN** an engineer reads the Session and state page
- **THEN** it describes exercise 15, says that it needs no Docker and no Redis, and links to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/15-session-state`

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder


## ADDED Requirements

### Requirement: Session exercise moves the basket into typed session
Exercise 15 SHALL give the engineer a basket-and-checkout API whose Starter keeps each user's basket
in the server's own memory, keyed by a cookie, the way InProc session did. The engineer SHALL move
the basket into ASP.NET Core session, read and written as a typed object serialized to JSON.

#### Scenario: Basket survives between requests
- **WHEN** the tests add two items to a basket and then read it, with one client
- **THEN** the basket holds both items, with their quantities

#### Scenario: Basket is stored in session as JSON
- **WHEN** the tests read what the app wrote to the distributed cache after adding an item
- **THEN** it holds the basket as JSON

#### Scenario: Starter keeps the basket in server memory
- **WHEN** the tests run against the starter
- **THEN** the session tests fail, each with a message that names what's wrong and the TODO that
  fixes it

### Requirement: Session exercise works across two instances
Exercise 15 SHALL run as two instances in the tests, sharing one distributed cache. The engineer
SHALL make a basket started on one instance readable on the other, by having both share a Data
Protection key ring and application name taken from configuration.

#### Scenario: Request moves to the other instance
- **WHEN** the tests add an item through one instance, then read the basket through the other with
  the same cookie
- **THEN** the second instance returns the same basket

#### Scenario: Exercise needs no Docker
- **WHEN** an engineer runs exercise 15's tests on a machine without Docker or Redis
- **THEN** they run, because both instances run in the test process and share an in-memory
  distributed cache

### Requirement: Session exercise moves the placed order out of session
Exercise 15's Starter SHALL keep the placed order only in session. The engineer SHALL save it to the
app's order store at checkout, empty the basket, and keep at most the order's id in session.

#### Scenario: Order outlives the session
- **WHEN** the tests check out, then fetch the order by its id with a new client that has no
  session cookie
- **THEN** the order is returned, with its items and total

#### Scenario: Checkout empties the basket
- **WHEN** the tests read the basket after checking out
- **THEN** it is empty
