# Spec Delta

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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: Dependency Injection exercise practises lifetimes
Exercise 02 SHALL have the engineer register services so that their lifetimes behave as follows:
- a singleton keeps one instance for the life of the app;
- a scoped service gives one instance per request, the same everywhere within that request and
  different across requests.

Its tests SHALL check both through HTTP requests to the app.

#### Scenario: Singleton survives across requests
- **WHEN** the engineer's completed app receives three requests to its visit-counting endpoint
- **THEN** the responses report counts 1, 2 and 3

#### Scenario: Scoped service is per request
- **WHEN** a single request resolves the scoped service in two places
- **THEN** both get the same instance, and a second request gets a different one

### Requirement: Middleware exercise practises custom middleware and order
Exercise 03 SHALL have the engineer write middleware that gives every response a correlation ID,
reusing the incoming ID when the request carries one. The ID SHALL also appear on responses that other
middleware short-circuits, which depends on pipeline order. It SHALL appear on error responses from the
exception handler too. The tests SHALL check each of these through HTTP requests.

#### Scenario: Correlation ID generated
- **WHEN** a request without an `X-Correlation-Id` header reaches the completed app
- **THEN** the response carries a non-empty `X-Correlation-Id` header

#### Scenario: Incoming correlation ID echoed
- **WHEN** a request carries `X-Correlation-Id: abc-123`
- **THEN** the response carries `X-Correlation-Id: abc-123`

#### Scenario: Correlation ID on a short-circuited response
- **WHEN** maintenance mode is switched on in configuration and a request arrives
- **THEN** the maintenance middleware answers 503 without reaching an endpoint, and the response still
  carries the `X-Correlation-Id` header

#### Scenario: Correlation ID survives an unhandled exception
- **WHEN** a request reaches an endpoint that throws
- **THEN** the response is a 500 Problem Details response that still carries the `X-Correlation-Id`
  header
