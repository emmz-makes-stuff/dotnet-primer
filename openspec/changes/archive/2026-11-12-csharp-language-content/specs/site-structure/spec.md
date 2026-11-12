## MODIFIED Requirements

### Requirement: Cheatsheets per area
The site SHALL have a cheatsheets section, with cheatsheets reachable from the navigation. The
subject-area page a cheatsheet belongs to SHALL link to it, and the cheatsheet SHALL link back to that
page. The site delivers fifteen cheatsheets: common `dotnet` CLI commands, Configuration, Dependency
Injection, Middleware, Logging, Tracing, Metrics, EF Core, gRPC, Testing, Security, Async &
performance, Containers, Orchestration & AWS, and C# language.

#### Scenario: dotnet CLI cheatsheet is reachable
- **WHEN** a reader opens the .NET 10 and `dotnet` CLI area page
- **THEN** it links to the `dotnet` CLI cheatsheet, which is also listed in the cheatsheets navigation

#### Scenario: Topic cheatsheets are reachable both ways
- **WHEN** a reader opens the Configuration, Dependency Injection, Middleware, Logging, Tracing,
  Metrics, EF Core, gRPC, Testing, Security & auth, Async & performance, Containers or Orchestration
  & AWS page, or the C# language section's first page
- **THEN** it links to that topic's cheatsheet, the cheatsheet is listed in the cheatsheets navigation,
  and the cheatsheet links back to the topic page

#### Scenario: Cheatsheets are linked from the introduction
- **WHEN** a reader opens the introduction page
- **THEN** it links to every cheatsheet

## ADDED Requirements

### Requirement: C# language section pages
The C# language section SHALL have four pages, listed in its navigation group in this order: Modern
C# at a glance, Null safety, Types, and Patterns. Each page SHALL link to the previous and next pages
within the group only, never into the learning path. The introduction SHALL link to all four.

#### Scenario: Reader opens the C# group
- **WHEN** a reader views the site navigation
- **THEN** the C# language group lists Modern C# at a glance, Null safety, Types and Patterns, in
  that order, apart from the learning path

#### Scenario: Reader moves through the section
- **WHEN** a reader reaches the end of the Null safety page
- **THEN** it offers links back to Modern C# at a glance and on to Types, and no link to a
  learning-path page as next or previous

#### Scenario: Reader starts from the introduction
- **WHEN** a reader opens the introduction page
- **THEN** it links to each of the four C# pages
