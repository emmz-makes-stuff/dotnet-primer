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

#### Scenario: Engineer finds the Observability exercises
- **WHEN** an engineer reads the Logging, Tracing or Metrics page
- **THEN** it describes exercise 04, 05 or 06 respectively, with a link to
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/04-logging`,
  `…/05-tracing` or `…/06-metrics`

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

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
