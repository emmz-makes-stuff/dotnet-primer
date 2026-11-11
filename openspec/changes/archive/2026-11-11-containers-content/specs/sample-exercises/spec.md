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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

### Requirement: EF Core exercise tests use a real SQL Server
Exercise 07's tests SHALL run against a real SQL Server that Testcontainers starts. Its tests and
exercise 12's are the only ones that need Docker. The samples test gate, locally and in CI, SHALL
run them.

#### Scenario: Gate runs the database tests
- **WHEN** the samples test gate runs with Docker available
- **THEN** exercise 07's solution tests start a SQL Server container, pass, and remove the container

#### Scenario: Docker isn't running
- **WHEN** the engineer runs the tests without a running Docker engine
- **THEN** the tests fail with a message that says Docker is needed, and points to the Docker page

## ADDED Requirements

### Requirement: Containers exercise fixes the app for containers
Exercise 12 SHALL give the engineer an orders API, with its orders in memory and no database, whose
code doesn't suit a container. The engineer SHALL make it listen on the port the container's
environment sets, instead of one hard-coded in the code, and SHALL make a setting that an
environment variable can't override take the variable's value.

#### Scenario: App answers on the container's port
- **WHEN** the tests run the app's image with no port settings of their own
- **THEN** the app answers on the port the .NET base image sets, 8080

#### Scenario: Environment variable overrides a setting
- **WHEN** the tests run the app's image with an environment variable for the exercise's setting
- **THEN** the app's response shows the variable's value, not the value in `appsettings.json`

#### Scenario: Starter ignores the environment
- **WHEN** the tests run against the starter
- **THEN** the port and setting tests fail, each with a message that names what's wrong and the
  TODO that fixes it

### Requirement: Containers exercise fixes the SDK-published image
In exercise 12, the engineer SHALL fix the image that `dotnet publish` builds without a Dockerfile,
using the project's container properties only, so that it runs as a non-root user and is built on a
chiseled base image with no shell or package manager.

#### Scenario: SDK image runs as a non-root user
- **WHEN** the tests build the SDK-published image and run it
- **THEN** the app's process isn't root, and a version that sets the image's user to root fails the
  test with a message that says so

#### Scenario: SDK image is chiseled
- **WHEN** the tests check the SDK-published image
- **THEN** it has no shell, and a version built on the full runtime base fails the test with a
  message that names its TODO

### Requirement: Containers exercise fixes the Dockerfile
In exercise 12, the engineer SHALL turn a single-stage Dockerfile on the SDK image into a
multi-stage one, which builds on the SDK image and runs on a chiseled runtime image as a non-root
user. The tests SHALL build the image from the exercise's own Dockerfile.

#### Scenario: Dockerfile image runs without the SDK
- **WHEN** the tests build the image from the Dockerfile and check it
- **THEN** it has no .NET SDK and no shell, and its process isn't root

#### Scenario: Single-stage Dockerfile is caught
- **WHEN** the tests run against the starter's Dockerfile
- **THEN** the Dockerfile tests fail, with a message that says the image still holds the SDK, and
  names the TODO that fixes it

#### Scenario: Both images pass with the Solution
- **WHEN** the tests run against the Solution
- **THEN** every test passes, for both the SDK-published image and the Dockerfile image
