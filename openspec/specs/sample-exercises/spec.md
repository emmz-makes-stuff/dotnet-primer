# sample-exercises Specification

## Purpose

Defines the companion exercise code that engineers clone and run locally to practise what the site
teaches. Every exercise has a starter, a reference solution and tests, and they all build on a pinned
.NET 10 SDK.

## Requirements

### Requirement: Exercise layout
Each exercise SHALL live in its own numbered folder under `samples/exercises/`. It SHALL contain a
starter project, a solution project, and tests that can run against either one. The starter SHALL mark
what the engineer must complete with `TODO` comments.

#### Scenario: Engineer opens an exercise
- **WHEN** an engineer opens `samples/exercises/01-configuration/`
- **THEN** they find a starter project with `TODO` markers, a solution project, and tests they can run
  against the starter

### Requirement: Practice, not assessment
Exercises SHALL be for self-checking practice only. Running the tests against the starter SHALL report
which behaviours are incomplete, and nothing is recorded or submitted anywhere.

#### Scenario: Engineer checks their progress
- **WHEN** an engineer runs the tests against the starter before completing the `TODO`s
- **THEN** the tests fail, with messages that point to the behaviour still to be completed, and no
  result leaves the machine

#### Scenario: Engineer completes the exercise
- **WHEN** an engineer completes the starter's `TODO`s correctly and runs its tests
- **THEN** the tests pass

### Requirement: Pinned .NET 10 SDK
Building or testing anything under `samples/` SHALL use a .NET SDK 10.0.4xx feature band, whatever newer
SDK is installed as the machine default. If no matching SDK is installed, the build SHALL fail with an
error that names the required version. The samples README SHALL tell engineers how to install the
pinned SDK with `dotnetup`.

#### Scenario: Machine default is a newer SDK
- **WHEN** an engineer whose default SDK is a .NET 11 preview builds the samples
- **THEN** the build uses an installed .NET 10.0.4xx SDK and targets `net10.0`

#### Scenario: Required SDK missing
- **WHEN** no 10.0.4xx SDK is installed
- **THEN** `dotnet` fails and reports the required SDK version, instead of building with another SDK

#### Scenario: Engineer installs the pinned SDK
- **WHEN** an engineer follows the samples README prerequisites from the repository root
- **THEN** `dotnetup` installs an SDK that satisfies `global.json`, and `dotnet --version` in the repo
  reports a 10.0.4xx SDK

### Requirement: Samples gates
Every starter, solution and test project SHALL compile with warnings treated as errors. The tests SHALL
pass against every solution project. Starter tests are excluded from the passing requirement. Samples
code SHALL pass the formatter's check mode.

#### Scenario: Gates on a clean checkout
- **WHEN** the samples build, test and format gates run on a clean checkout
- **THEN** all projects compile with no warnings, every solution's tests pass, the format check reports
  no changes, and each gate exits zero

#### Scenario: Solution regresses
- **WHEN** a change makes a solution project's tests fail
- **THEN** the samples test gate exits non-zero

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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

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

### Requirement: Logging exercise practises structured, safe logging
Exercise 04 SHALL have the engineer add logging to an order-placing endpoint:
- a successful order is logged once, at Information, with the order ID and item count as separate
  structured properties;
- a declined payment is logged once, at Warning, with the order ID and the decline reason;
- the customer's email address never appears in any log entry.

Its tests SHALL check the captured log entries, without Docker.

#### Scenario: Order placed is logged with properties
- **WHEN** the engineer's completed app accepts an order for 3 items
- **THEN** exactly one Information entry for it is captured, whose structured properties include the
  order ID and an item count of 3

#### Scenario: Declined payment is logged once
- **WHEN** an order's payment is declined
- **THEN** exactly one Warning entry for it is captured, with the order ID and the decline reason,
  and no Error entry is logged for that order

#### Scenario: Personal data stays out of the logs
- **WHEN** any order is placed, whether accepted or declined
- **THEN** no captured log message or property contains the customer's email address

### Requirement: Tracing exercise practises custom spans and error recording
Exercise 05 SHALL have the engineer trace the order-placing endpoint:
- a span for placing the order, a child of the incoming request's span, with the order ID and item
  count as attributes;
- a child span for the payment step;
- when the payment step fails, its span is marked as an error and records the exception.

Its tests SHALL check the recorded spans, without Docker.

#### Scenario: Order span joins the request trace
- **WHEN** the engineer's completed app accepts an order
- **THEN** a span for placing it is recorded in the same trace as the request, as a child of the
  request's span, with the order ID and item count as attributes

#### Scenario: Payment step has its own span
- **WHEN** an order is placed
- **THEN** a payment span is recorded as a child of the order span

#### Scenario: Failed payment marks the span
- **WHEN** the payment step throws
- **THEN** the payment span has error status, and carries an exception event with the exception's
  type and message

### Requirement: Metrics exercise practises instruments and low-cardinality tags
Exercise 06 SHALL have the engineer add metrics to the order-placing endpoint:
- a counter of orders placed, tagged with the payment method and the outcome;
- a histogram of order values, with its unit stated.

No order or customer identifier SHALL be used as a tag. Its tests SHALL check the recorded
measurements, without Docker.

#### Scenario: Orders are counted by bounded tags
- **WHEN** the engineer's completed app accepts two card orders and declines one voucher order
- **THEN** the order counter records 2 for card/accepted and 1 for voucher/declined

#### Scenario: Order values are recorded in a histogram
- **WHEN** an order worth 42.50 is accepted
- **THEN** the order-value histogram records 42.50, and the instrument declares its unit

#### Scenario: Identifiers are not tags
- **WHEN** any order is placed
- **THEN** no recorded measurement carries an order ID or customer identifier tag
