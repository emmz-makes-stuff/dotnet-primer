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
error that names the required version.

#### Scenario: Machine default is a newer SDK
- **WHEN** an engineer whose default SDK is a .NET 11 preview builds the samples
- **THEN** the build uses an installed .NET 10.0.4xx SDK and targets `net10.0`

#### Scenario: Required SDK missing
- **WHEN** no 10.0.4xx SDK is installed
- **THEN** `dotnet` fails and reports the required SDK version, instead of building with another SDK

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
