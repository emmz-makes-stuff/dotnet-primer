# content-page-standards Specification

## Purpose

Sets the conventions every content page on the primer site must follow. Readers can always reach the
authoritative Microsoft documentation, report problems with a page, and copy code samples easily.

## Requirements

### Requirement: Content pages link to Microsoft Learn
Every content page SHALL include at least one link to official Microsoft documentation on
`learn.microsoft.com`. Content pages are the introduction, the area pages, the standalone sections and
the cheatsheets. The links SHALL be shown in a consistent "on Microsoft Learn" block. A content page
with no such link SHALL fail the build or the site test gate.

#### Scenario: Area page shows Microsoft Learn links
- **WHEN** a reader opens any content page
- **THEN** the page shows an "on Microsoft Learn" block with at least one link to a
  `https://learn.microsoft.com/` URL

#### Scenario: Missing Microsoft Learn link is caught
- **WHEN** an author adds a content page that declares no Microsoft Learn link
- **THEN** the site build or the site test gate fails and names the page

### Requirement: Report-a-problem link on every page
Every content page SHALL include a "Report a problem" link. The link SHALL open a new issue on the
`emmz-makes-stuff/dotnet-primer` GitHub repository, with a title and body prefilled to identify the page
by its title and full URL.

#### Scenario: Reader reports an error
- **WHEN** a reader selects "Report a problem" on the Dependency Injection page
- **THEN** the browser opens `https://github.com/emmz-makes-stuff/dotnet-primer/issues/new`, with a
  prefilled title naming the page and a body containing that page's full
  `https://dotnet-primer.emmz.dev/` URL

### Requirement: Copyable, highlighted code samples
Code blocks on the site SHALL be syntax-highlighted for their declared language, including C#, shell,
JSON, XML and Dockerfile. Each code block SHALL have a one-click copy control that copies the code
without line numbers or annotations.

#### Scenario: Copying a command from a cheatsheet
- **WHEN** a reader selects the copy control on a code block in the `dotnet` CLI cheatsheet
- **THEN** the block's code text, and nothing else, is placed on the clipboard

### Requirement: Internal links resolve
Every link from one site page to another SHALL point to a page that exists in the built site. A broken
internal link SHALL fail the site test gate.

#### Scenario: Broken internal link is caught
- **WHEN** a page links to a site path with no built page
- **THEN** the site test gate fails and reports the source page and the broken target
