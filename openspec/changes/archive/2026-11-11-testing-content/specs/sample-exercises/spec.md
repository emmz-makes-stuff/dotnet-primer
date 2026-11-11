## MODIFIED Requirements

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

#### Scenario: Undescribed exercise is caught
- **WHEN** a folder under `samples/exercises/` is not linked from any site page
- **THEN** the site test gate fails and names the exercise folder

## ADDED Requirements

### Requirement: Testing exercise layout
A testing exercise, in which the engineer writes tests, SHALL instead contain the app under test,
already written; a starter test project with `TODO` comments; a solution test project; the seeded
bugs; and a checker that runs a test project against the app and against each seeded bug.

#### Scenario: Engineer opens the testing exercise
- **WHEN** an engineer opens `samples/exercises/09-testing/`
- **THEN** they find the app to test, a starter test project with `TODO` markers, a solution test
  project, and a checker they can run against the starter tests

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
