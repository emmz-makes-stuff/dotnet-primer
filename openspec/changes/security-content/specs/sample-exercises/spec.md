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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: Security exercise practises JWT bearer authentication
Exercise 10 SHALL have the engineer secure an orders API, with an in-memory store and no database,
Docker or identity provider, by validating JWT bearer tokens configured from settings. The tests
SHALL mint their own signed tokens with a key they give the app's configuration.

#### Scenario: Valid token is accepted
- **WHEN** the tests call a protected endpoint with a valid token for the configured issuer and
  audience
- **THEN** the call succeeds

#### Scenario: Missing or bad token is refused
- **WHEN** the tests call a protected endpoint with no token, an expired token, a token signed with
  another key, or a token for another audience
- **THEN** each call gets 401

#### Scenario: Health stays public
- **WHEN** the tests call the health endpoint with no token
- **THEN** it answers 200

#### Scenario: API is still open
- **WHEN** the tests run against the starter before authentication is added
- **THEN** the token tests fail with a message that says a request with no token was let through,
  and to add JWT bearer authentication

### Requirement: Security exercise practises policy-based authorization
In exercise 10, the engineer SHALL make endpoints private by default and add three checks: a scope
policy for placing orders, a role policy for refunds, and a resource-based check so a customer reads
only their own orders while support staff can read any order.

#### Scenario: Placing an order needs the scope
- **WHEN** the tests place an order with a valid token without the write scope, and then with it
- **THEN** the first call gets 403 and the second succeeds

#### Scenario: Customer reads orders
- **WHEN** a customer's token reads one of their own orders, and then another customer's order
- **THEN** the first call succeeds and the second gets 403

#### Scenario: Support reads and refunds
- **WHEN** a token with the support role reads another customer's order and refunds it
- **THEN** both calls succeed, and a customer's token gets 403 for the refund

#### Scenario: New endpoint is private by default
- **WHEN** the tests call an endpoint that has no authorization metadata of its own, with no token
- **THEN** it gets 401
