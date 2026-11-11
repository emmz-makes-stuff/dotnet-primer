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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: Modern C# exercise practises null safety
Exercise 14 SHALL give the engineer a small order-pricing library, written in the style of C# 3 and
with no web host, in which nullable reference types are turned off. The engineer SHALL turn them on,
annotate what can be null and what can't, and make pricing an order whose customer is missing work
without an exception.

#### Scenario: Engineer annotates the library
- **WHEN** the tests read the library's nullability annotations
- **THEN** looking up a customer is annotated as possibly returning null, a customer's name as never
  null, and a customer's email as possibly null

#### Scenario: Customer is missing
- **WHEN** the tests price an order whose customer can't be found
- **THEN** it's priced without any customer discount, and nothing throws

#### Scenario: Starter's null handling is wrong
- **WHEN** the tests run against the starter
- **THEN** the null-safety tests fail, each with a message that names what's wrong, its TODO, and the
  Null safety page

### Requirement: Modern C# exercise practises records and immutability
The engineer SHALL make exercise 14's order lines values: two lines with the same contents are
equal, and each member is required and can be set only when the line is created. The engineer SHALL
make applying a discount to an order return a new order, and leave the original unchanged.

#### Scenario: Two lines with the same contents
- **WHEN** the tests compare two order lines created with the same values
- **THEN** they're equal

#### Scenario: Engineer checks the members
- **WHEN** the tests read an order line's members
- **THEN** each is required, and can be set only during initialization

#### Scenario: Discount leaves the original alone
- **WHEN** the tests apply a discount to an order
- **THEN** they get a new order with the discount, and the original order is unchanged

#### Scenario: Starter's types are wrong
- **WHEN** the tests run against the starter
- **THEN** the type tests fail, each with a message that names what's wrong, its TODO, and the Types
  page

### Requirement: Modern C# exercise practises patterns
The engineer SHALL rewrite exercise 14's discount rules, an if/else chain that gets some cases wrong,
as a switch expression with patterns. The tests SHALL check each rule's result, not the syntax that
produced it, and the exercise SHALL say so.

#### Scenario: Each rule gives its discount
- **WHEN** the tests price orders that each match one discount rule, including one that depends on
  the order's lines, such as an order with a single line
- **THEN** each gets that rule's discount

#### Scenario: Starter's rules are wrong
- **WHEN** the tests run against the starter
- **THEN** the rule tests fail for the cases the if/else chain gets wrong, each with a message that
  names the case, its TODO, and the Patterns page

#### Scenario: Exercise needs no Docker
- **WHEN** an engineer runs exercise 14's tests on a machine without Docker
- **THEN** they run, because the library has no web host and no database
