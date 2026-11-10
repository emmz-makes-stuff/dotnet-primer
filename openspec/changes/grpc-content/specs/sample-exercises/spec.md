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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: gRPC exercise practises the contract and unary calls
Exercise 08 SHALL have the engineer serve an orders gRPC service from a `.proto` contract, with an
in-memory order store and no database or Docker. The engineer's service SHALL place an order and get
an order by id, failing with the gRPC status code that fits each error. The tests SHALL call it as a
gRPC client would, through the published contract.

#### Scenario: Order is placed and read back
- **WHEN** the tests place a valid order and then get it by the returned id
- **THEN** they get the order back with its customer, lines and total

#### Scenario: Invalid order is rejected
- **WHEN** the tests place an order with no customer email, no lines, or a line with a quantity below 1
- **THEN** the call fails with `InvalidArgument`, and nothing is stored

#### Scenario: Unknown order
- **WHEN** the tests get an order id that doesn't exist
- **THEN** the call fails with `NotFound`

#### Scenario: Service isn't mapped yet
- **WHEN** the tests run against the starter before the service is registered and mapped
- **THEN** each test fails with a message that says to register gRPC and map the orders service

### Requirement: gRPC exercise practises streaming and deadlines
In exercise 08, the engineer SHALL add a server-streaming RPC to the `.proto` that sends an order's
status updates, and implement it. The stream SHALL stop when the client's deadline passes or the
client cancels, and the service SHALL stop producing updates when that happens.

#### Scenario: Client reads the status updates
- **WHEN** the tests call the streaming RPC for a placed order
- **THEN** they read each status update in order, and the stream ends after the last one

#### Scenario: Deadline passes mid-stream
- **WHEN** the tests call the streaming RPC with a deadline shorter than the updates take
- **THEN** the call fails with `DeadlineExceeded`, and the service stops producing updates soon after

#### Scenario: Stream for an unknown order
- **WHEN** the tests call the streaming RPC for an order id that doesn't exist
- **THEN** the call fails with `NotFound`

#### Scenario: Streaming RPC missing from the contract
- **WHEN** the tests call the streaming RPC on a service whose `.proto` doesn't define it yet
- **THEN** the test fails with a message that says to add the RPC to the `.proto`, matching the
  published contract's names and field numbers
