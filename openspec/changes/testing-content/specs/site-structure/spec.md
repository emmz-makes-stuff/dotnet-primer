## MODIFIED Requirements

### Requirement: Cheatsheets per area
The site SHALL have a cheatsheets section, with cheatsheets reachable from the navigation. The
subject-area page a cheatsheet belongs to SHALL link to it, and the cheatsheet SHALL link back to that
page. The site delivers ten cheatsheets: common `dotnet` CLI commands, Configuration, Dependency
Injection, Middleware, Logging, Tracing, Metrics, EF Core, gRPC and Testing.

#### Scenario: dotnet CLI cheatsheet is reachable
- **WHEN** a reader opens the .NET 10 and `dotnet` CLI area page
- **THEN** it links to the `dotnet` CLI cheatsheet, which is also listed in the cheatsheets navigation

#### Scenario: Topic cheatsheets are reachable both ways
- **WHEN** a reader opens the Configuration, Dependency Injection, Middleware, Logging, Tracing,
  Metrics, EF Core, gRPC or Testing page
- **THEN** it links to that topic's cheatsheet, the cheatsheet is listed in the cheatsheets navigation,
  and the cheatsheet links back to the topic page

#### Scenario: Cheatsheets are linked from the introduction
- **WHEN** a reader opens the introduction page
- **THEN** it links to every cheatsheet
