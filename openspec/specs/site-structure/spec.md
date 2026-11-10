# site-structure Specification

## Purpose

Defines how the primer site is organised. Readers can follow a learning path from a readable
introduction, drill into any subject area, look things up in cheatsheets, and search the whole site.

## Requirements

### Requirement: Introduction page with drill-down links
The site SHALL serve an introduction page at its root URL. The page SHALL explain who the primer is for
and how it is organised. It SHALL link to every subject area and to both standalone sections.

#### Scenario: Reader lands on the site root
- **WHEN** a reader opens `https://dotnet-primer.emmz.dev/`
- **THEN** they see the introduction page, with a link to each subject area page and to the C#
  language and coming-from-.NET-3.5 sections

### Requirement: One page per subject area
The site SHALL have a page for each subject area in scope:
- .NET 10 and the `dotnet` CLI
- ASP.NET Core, Configuration, Dependency Injection, Middleware
- Logging, Tracing, Metrics
- EF Core, gRPC, Testing, Security & auth, Async & performance
- Containers, and Orchestration & AWS hosting

#### Scenario: Every subject area is reachable
- **WHEN** a reader follows the site navigation
- **THEN** each subject area listed above has its own page that the navigation reaches

### Requirement: Learning path with ASP.NET Core as the spine
The site navigation SHALL present the subject areas as an ordered learning path. ASP.NET Core SHALL be
the primary use case that introduces the other topics. Configuration, Dependency Injection and
Middleware SHALL come first after the ASP.NET Core entry point. Each page on the path SHALL link to the
next page and the previous page.

#### Scenario: Path order starts with the ASP.NET Core spine
- **WHEN** a reader views the learning-path navigation
- **THEN** the ASP.NET Core entry page comes before Configuration, Configuration before Dependency
  Injection, and Dependency Injection before Middleware, and all three precede the remaining areas

#### Scenario: Moving along the path
- **WHEN** a reader reaches the end of a learning-path page
- **THEN** the page offers links to the previous and next pages on the path

### Requirement: Standalone C# and coming-from-.NET-3.5 sections
The site SHALL have a C# language section and a "coming from .NET 3.5" section. Both SHALL sit outside
the ordered learning path in the navigation. The coming-from-.NET-3.5 section SHALL be where old-to-new
mappings live, so subject-area pages can teach modern .NET on its own terms.

#### Scenario: Standalone sections are distinct from the path
- **WHEN** a reader views the site navigation
- **THEN** the C# language and coming-from-.NET-3.5 sections appear as separate groups, not as steps in
  the learning path

### Requirement: Cheatsheets per area
The site SHALL have a cheatsheets section, with cheatsheets reachable from the navigation. The
subject-area page a cheatsheet belongs to SHALL link to it, and the cheatsheet SHALL link back to that
page. The site delivers seven cheatsheets: common `dotnet` CLI commands, Configuration, Dependency
Injection, Middleware, Logging, Tracing and Metrics.

#### Scenario: dotnet CLI cheatsheet is reachable
- **WHEN** a reader opens the .NET 10 and `dotnet` CLI area page
- **THEN** it links to the `dotnet` CLI cheatsheet, which is also listed in the cheatsheets navigation

#### Scenario: Topic cheatsheets are reachable both ways
- **WHEN** a reader opens the Configuration, Dependency Injection, Middleware, Logging, Tracing or
  Metrics page
- **THEN** it links to that topic's cheatsheet, the cheatsheet is listed in the cheatsheets navigation,
  and the cheatsheet links back to the topic page

#### Scenario: Cheatsheets are linked from the introduction
- **WHEN** a reader opens the introduction page
- **THEN** it links to every cheatsheet

### Requirement: Site-wide search
The site SHALL offer full-text search across all pages, and it SHALL work on static hosting with no
server-side component.

#### Scenario: Searching for a topic
- **WHEN** a reader searches for "Dependency Injection"
- **THEN** the results include the Dependency Injection area page, and selecting a result opens it

### Requirement: Docker page in Getting started
The navigation's Getting started group SHALL list a Docker page straight after the .NET 10 and
`dotnet` CLI page, so a reader sets up .NET and then Docker before the learning path.

#### Scenario: Reader follows Getting started in order
- **WHEN** a reader opens the Getting started group in the sidebar
- **THEN** it lists ".NET 10 and the dotnet CLI" and then "Docker", and the Docker page is reachable
  from it
