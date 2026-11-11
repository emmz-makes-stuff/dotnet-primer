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

### Requirement: Testing exercise layout
A testing exercise, in which the engineer writes tests, SHALL instead contain the app under test,
already written; a starter test project with `TODO` comments; a solution test project; the seeded
bugs; and a checker that runs a test project against the app and against each seeded bug.

#### Scenario: Engineer opens the testing exercise
- **WHEN** an engineer opens `samples/exercises/09-testing/`
- **THEN** they find the app to test, a starter test project with `TODO` markers, a solution test
  project, and a checker they can run against the starter tests

### Requirement: Practice, not assessment
Exercises SHALL be for self-checking practice only. Running the tests against the starter SHALL report
which behaviours are incomplete, and nothing is recorded or submitted anywhere. In a testing exercise,
running the checker against the starter tests SHALL report which seeded bugs the engineer's tests
miss.

#### Scenario: Engineer checks their progress
- **WHEN** an engineer runs the tests against the starter before completing the `TODO`s
- **THEN** the tests fail, with messages that point to the behaviour still to be completed, and no
  result leaves the machine

#### Scenario: Engineer completes the exercise
- **WHEN** an engineer completes the starter's `TODO`s correctly and runs its tests
- **THEN** the tests pass

#### Scenario: Engineer checks their tests
- **WHEN** an engineer runs the testing exercise's checker against their starter tests
- **THEN** it reports each seeded bug as caught or missed, points each missed one at its `TODO`, and
  no result leaves the machine

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
code SHALL pass the formatter's check mode. A testing exercise's solution tests SHALL pass against its
app and SHALL fail against each of its seeded bugs.

#### Scenario: Gates on a clean checkout
- **WHEN** the samples build, test and format gates run on a clean checkout
- **THEN** all projects compile with no warnings, every solution's tests pass, the format check reports
  no changes, and each gate exits zero

#### Scenario: Solution regresses
- **WHEN** a change makes a solution project's tests fail
- **THEN** the samples test gate exits non-zero

#### Scenario: Seeded bug becomes uncatchable
- **WHEN** a change leaves a testing exercise's solution tests passing against one of its seeded bugs
- **THEN** the samples test gate exits non-zero and names that bug

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

### Requirement: EF Core exercise practises the model and migrations
Exercise 07 SHALL have the engineer give the orders app a SQL Server database through EF Core:
- an `OrdersDbContext`, registered with `AddDbContextPool` (one instance per request, reused from a
  pool), whose connection string comes from configuration;
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

The app SHALL also come with request validation and a `GET /orders/{id}` endpoint, already written
in both the starter and the solution.

#### Scenario: Invalid order is rejected
- **WHEN** an order arrives with a missing or over-long email, a null item, a missing or over-long
  SKU, a quantity below 1, a unit price that is negative or has more than 2 decimal places, or a
  total too big for `decimal(10,2)`
- **THEN** the app returns 400 and saves nothing

#### Scenario: Placed order can be read back
- **WHEN** a client follows the `Location` header of a placed order's 201 response
- **THEN** it gets the order with its total and lines, and an unknown id returns 404

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

### Requirement: Testing exercise seeds a bug for each technique
Exercise 09 SHALL give the engineer a working orders API, in memory with no Docker, to write tests
for. It SHALL ship four seeded bugs, each a variant of the API that is wrong in one behaviour, and
each catchable with the technique its starter `TODO` names: a plain unit test, a test double, a
fake time provider, or an integration test through `WebApplicationFactory`.

#### Scenario: Discount boundary bug
- **WHEN** the seeded bug for the business rule is in place
- **THEN** an order totalling exactly the discount threshold gets no discount, where the correct API
  gives it

#### Scenario: Dependency bug
- **WHEN** the seeded bug for the dependency is in place
- **THEN** cancelling an order no longer tells the warehouse to release its stock

#### Scenario: Time bug
- **WHEN** the seeded bug for the time rule is in place
- **THEN** the cancel window is judged by the system clock instead of the app's time provider, so an
  order cancelled after the window closes, as the time provider tells it, is still cancelled

#### Scenario: Registration bug
- **WHEN** the seeded bug for the registration is in place
- **THEN** an order placed in one request can't be found in the next

#### Scenario: Correct API passes the engineer's tests
- **WHEN** an engineer's tests are correct
- **THEN** they all pass against the correct API, so a failing test points at the test, not at the
  API

### Requirement: Testing exercise checker reports caught and missed bugs
Exercise 09's checker SHALL run a test project first against the correct API, then against each
seeded bug, one at a time. It SHALL report the result in the engineer's terms, without needing any
tool beyond the pinned .NET SDK.

#### Scenario: Engineer's tests fail against the correct API
- **WHEN** some of the engineer's tests fail against the correct API
- **THEN** the checker names the failing tests, says the tests are wrong rather than the API, and
  doesn't run the seeded bugs

#### Scenario: A bug is missed
- **WHEN** the engineer's tests pass against the correct API and against a seeded bug
- **THEN** the checker reports that bug as missed, with a hint that names its `TODO` and technique
  without giving the test away

#### Scenario: Every bug is caught
- **WHEN** the engineer's tests pass against the correct API and fail against every seeded bug
- **THEN** the checker reports all four caught and exits zero

#### Scenario: Starter as shipped
- **WHEN** the engineer runs the checker against the starter tests before completing any `TODO`
- **THEN** it reports every seeded bug missed and exits non-zero

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
