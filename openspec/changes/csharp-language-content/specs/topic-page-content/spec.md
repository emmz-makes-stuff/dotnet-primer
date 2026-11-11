## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page, a C# language page or a cheatsheet delivered with full content SHALL NOT show
the "Coming soon" placeholder note. If one of them shows the placeholder, the site test gate SHALL
fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection and Middleware pages and their three cheatsheets, the .NET 10 and `dotnet`
  CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages with their three
  cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its cheatsheet, the Testing
  page with its cheatsheet, the Security & auth page with its cheatsheet, the Async & performance
  page with its cheatsheet, the Containers page with its cheatsheet, the Orchestration & AWS page
  with its cheatsheet, and the four C# language pages with the C# cheatsheet

#### Scenario: Reader opens a finished page
- **WHEN** a reader opens the Dependency Injection page
- **THEN** the page has no "Coming soon" note

#### Scenario: Reader opens the finished dotnet CLI cheatsheet
- **WHEN** a reader opens the `dotnet` CLI cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished Observability page
- **WHEN** a reader opens the Logging, Tracing or Metrics page, or its cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished EF Core page
- **WHEN** a reader opens the EF Core page or the EF Core cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished gRPC page
- **WHEN** a reader opens the gRPC page or the gRPC cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Testing page
- **WHEN** a reader opens the Testing page or the Testing cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Security page
- **WHEN** a reader opens the Security & auth page or the Security cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Async page
- **WHEN** a reader opens the Async & performance page or the Async & performance cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Containers page
- **WHEN** a reader opens the Containers page or the Containers cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Orchestration page
- **WHEN** a reader opens the Orchestration & AWS page or the Orchestration & AWS cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished C# page
- **WHEN** a reader opens any of the four C# language pages, or the C# cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

### Requirement: C# language pages share a shape
Each C# language page SHALL open with what the reader will be able to do afterwards, explain its
features with code, and end with a "Pitfalls" section. It SHALL tag each feature it introduces with
the C# version that introduced it. The Null safety, Types and Patterns pages SHALL each have a
"Coming from C# 3" table before their Pitfalls, and SHALL end by pointing to Exercise 14.

#### Scenario: Reader wonders how new a feature is
- **WHEN** a reader meets a feature on a C# page, such as list patterns
- **THEN** the page says which C# version introduced it, and the version matches Microsoft Learn's
  "What's new in C#" pages

#### Scenario: Reader maps a C# 3 habit
- **WHEN** a reader who would write a C# 3 class with private fields and a constructor reads the
  Types page
- **THEN** its "Coming from C# 3" table points them to the modern form, and to the section that
  explains it

#### Scenario: Reader looks for practice
- **WHEN** a reader reaches the end of the Null safety, Types or Patterns page
- **THEN** it points to Exercise 14 on the section's first page

### Requirement: C# examples are compiled and measured
Every C# example on the C# language pages and the C# cheatsheet SHALL compile with the primer's
pinned .NET SDK and its warnings-as-errors settings, unless the page shows it as code that doesn't
compile. Every compiler message and program output a page shows SHALL come from a real build or run.

#### Scenario: Reader copies an example
- **WHEN** a reader copies a C# example from a C# page into a project on the primer's SDK
- **THEN** it compiles, apart from any example the page marks as not compiling, which fails with the
  message the page shows

#### Scenario: Reader compares a warning
- **WHEN** a C# page shows a compiler warning, such as a nullable warning
- **THEN** its code and text are the ones the compiler gave for that example

### Requirement: C# index covers modern C# at a glance
The C# language section's first page SHALL show one class written in C# 3 and in modern C#, side by
side, and then the syntax that removes ceremony from a file. It SHALL link to the C# cheatsheet near
the top, point to the Async & performance page for async and `Span<T>`, and describe Exercise 14 after
its Pitfalls.

#### Scenario: Reader compares the two versions
- **WHEN** a reader opens the first C# page
- **THEN** it shows the same class in C# 3 and in modern C#, and names each feature that changed it,
  with a link to the page that covers it

#### Scenario: Reader meets the top of a modern file
- **WHEN** a reader looks for a feature they meet at the top of a file or a method
- **THEN** the page covers file-scoped namespaces, implicit and global usings, top-level statements,
  target-typed `new()`, collection expressions, `params` collections, raw string literals, string
  interpolation, expression-bodied members, using declarations and local functions

#### Scenario: Reader looks for async
- **WHEN** a reader looks for `async` and `await` in the C# section
- **THEN** the first page points them to the Async & performance page

### Requirement: C# null safety page covers nullable reference types
The Null safety page SHALL explain nullable reference types: annotations, the warnings, the
null-handling operators, `is null` and `is not null`, the null-forgiving operator, the nullable
attributes, and null-conditional assignment. It SHALL show how to turn nullable on in an existing
codebase a file at a time, with the warnings that appear, from a real build.

#### Scenario: Reader turns nullable on in old code
- **WHEN** a reader follows the page's steps to turn nullable on in a file of C# 3-style code
- **THEN** the page shows the warnings the build gives, and how each is fixed

#### Scenario: Reader is tempted to silence a warning
- **WHEN** a reader reaches for the null-forgiving operator to make a warning go away
- **THEN** the page says when it's justified and when it hides a bug, and its Pitfalls name that
  mistake

### Requirement: C# types page covers records and constructors
The Types page SHALL explain records and record structs, `init` and `required` members,
`with`-expressions, primary constructors on classes and on records, tuples and deconstruction, the
`field` keyword, and extension members. It SHALL say how a class's primary constructor parameters
differ from a record's.

#### Scenario: Reader compares two records
- **WHEN** a reader compares two records with the same values
- **THEN** the page shows that they're equal, and what their `ToString` prints, from a real run

#### Scenario: Reader uses a primary constructor on a class
- **WHEN** a reader gives a class a primary constructor
- **THEN** the page says that its parameters are captured for the class's members, not exposed as
  properties as a record's are

### Requirement: C# patterns page covers pattern matching
The Patterns page SHALL explain `is` patterns, switch expressions, and property, relational, logical
and list patterns. It SHALL show an if/else chain rewritten as a switch expression, and what the
compiler says when a switch expression doesn't handle every input, from a real build.

#### Scenario: Reader rewrites an if/else chain
- **WHEN** a reader compares the page's if/else chain with its switch expression
- **THEN** both give the same results for every case the page lists

#### Scenario: Reader leaves a case out
- **WHEN** a switch expression on the page doesn't cover every input
- **THEN** the page shows the compiler's warning, and says what happens at run time when no arm
  matches

### Requirement: Topic pages link C# features to the C# section
A topic page that explains a C# feature in passing SHALL link to the C# language page that covers
that feature.

#### Scenario: Reader meets a primary constructor on the DI page
- **WHEN** a reader reads the Dependency Injection page's primary constructor example
- **THEN** it links to the Types page's section on primary constructors

#### Scenario: Reader meets nullable reference types on the CLI page
- **WHEN** a reader reads the CLI page's note on the `Nullable` setting
- **THEN** it links to the Null safety page
