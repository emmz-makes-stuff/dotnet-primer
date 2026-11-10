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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: EF Core exercise practises the model and migrations
Exercise 07 SHALL have the engineer give the orders app a SQL Server database through EF Core:
- an `OrdersDbContext`, registered as scoped, whose connection string comes from configuration;
- a model with orders and their lines, configured with a maximum-length customer email, a
  `decimal(10,2)` total and line price, a stored status, and a one-to-many relationship;
- an initial migration that matches the model.

#### Scenario: Migration matches the model
- **WHEN** the tests apply the engineer's migrations to an empty database
- **THEN** the migrations apply, and the model has no changes that a migration doesn't cover

#### Scenario: Model is configured
- **WHEN** the tests read the engineer's model
- **THEN** the customer email has a maximum length, the money columns are `decimal(10,2)`, and an
  order's lines are a one-to-many relationship

### Requirement: EF Core exercise practises saving, querying and change tracking
In exercise 07, the engineer's endpoints SHALL:
- save a placed order and its lines in one `SaveChanges`;
- list a customer's orders with each order's line count, in one SQL command;
- cancel an order by changing its status through change tracking.

#### Scenario: Order and lines are saved together
- **WHEN** the engineer's completed app accepts an order with 3 lines
- **THEN** the database holds the order and its 3 lines, with the total and prices stored exactly

#### Scenario: Listing orders avoids N+1
- **WHEN** a customer with 5 orders is listed
- **THEN** the response has each order's line count, and the app sent one SQL command for the list

#### Scenario: Cancelling uses change tracking
- **WHEN** an existing order is cancelled
- **THEN** its stored status is Cancelled, and cancelling an unknown order returns 404

### Requirement: EF Core exercise tests use a real SQL Server
Exercise 07's tests SHALL run against a real SQL Server that Testcontainers starts. It is the only
exercise whose tests need Docker. The samples test gate, locally and in CI, SHALL run them.

#### Scenario: Gate runs the database tests
- **WHEN** the samples test gate runs with Docker available
- **THEN** exercise 07's solution tests start a SQL Server container, pass, and remove the container

#### Scenario: Docker isn't running
- **WHEN** the engineer runs the tests without a running Docker engine
- **THEN** the tests fail with a message that says Docker is needed, and points to the Docker page
