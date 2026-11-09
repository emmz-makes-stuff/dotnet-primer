# Spec Delta

## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. That means the ASP.NET Core entry page, the Configuration, Dependency Injection and
Middleware pages and their three cheatsheets, and the .NET 10 and `dotnet` CLI page with the `dotnet`
CLI cheatsheet. If one of them shows the placeholder, the site test gate SHALL fail.

#### Scenario: Reader opens a finished page
- **WHEN** a reader opens the Dependency Injection page
- **THEN** the page has no "Coming soon" note

#### Scenario: Reader opens the finished dotnet CLI cheatsheet
- **WHEN** a reader opens the `dotnet` CLI cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

### Requirement: Getting started page orients a .NET 3.5 engineer
The .NET 10 and `dotnet` CLI page SHALL briefly explain how modern .NET relates to .NET Framework:
cross-platform rather than Windows-only, the yearly release cadence with LTS and STS releases, and
SDK-style project files in place of the old ones. It SHALL also explain the difference between an
SDK, a runtime and a target framework.

#### Scenario: Reader asks which .NET to target
- **WHEN** a reader coming from .NET 3.5 reads the page
- **THEN** it says what LTS and STS mean, and why `net10.0` is the target framework used throughout
  the primer

### Requirement: Getting started page teaches dotnetup for installation
The page SHALL teach `dotnetup` as the way to install and manage .NET SDKs and runtimes on a
development machine:
- first-time setup;
- installing by channel or exact version;
- installing the SDK a repository's `global.json` asks for;
- listing, updating and uninstalling.

It SHALL say that `dotnetup` is in public preview, that production servers and container images get
.NET in other ways, and link to Microsoft's other install methods.

#### Scenario: Reader sets up a new machine
- **WHEN** a reader with no .NET installed follows the page
- **THEN** they can install a .NET 10 SDK with `dotnetup` and confirm it with `dotnet --version`

#### Scenario: Reader sees the preview caution
- **WHEN** a reader reads the install section
- **THEN** it states that `dotnetup` is in public preview, and links to the other installation
  methods on Microsoft Learn

### Requirement: Getting started page covers projects and the CLI
The page SHALL explain:
- `global.json` SDK pinning and roll-forward;
- the SDK-style project file (`TargetFramework`, implicit usings, nullable reference types,
  `PackageReference`);
- shared build settings and central package management;
- the everyday `dotnet` CLI commands for creating, building, running, testing, adding packages and
  references, and publishing, with solution files.

#### Scenario: Reader opens a primer sample project
- **WHEN** a reader compares a `.csproj` under `samples/` with the page
- **THEN** every element in it is explained on the page, or by the shared build settings the page
  describes

### Requirement: Getting started page ends with a setup check
The page SHALL end with a recipe that confirms the reader's setup. The recipe SHALL clone the
repository, install the SDK the samples pin, and run exercise 01's solution tests.

#### Scenario: Reader checks their setup
- **WHEN** a reader follows the setup-check recipe on a machine set up as the page describes
- **THEN** exercise 01's solution tests pass

### Requirement: dotnet CLI cheatsheet coverage
The `dotnet` CLI cheatsheet SHALL group the everyday commands in copyable blocks, covering:
- installing and listing SDKs and runtimes with `dotnetup`;
- SDK selection and `global.json`;
- creating projects and solutions;
- building, running, watching and testing;
- packages and references;
- tools;
- user secrets;
- EF Core migrations;
- formatting;
- publishing.

#### Scenario: Engineer needs a command quickly
- **WHEN** an engineer opens the cheatsheet to add a NuGet package or run EF Core migrations
- **THEN** the command is on the page in a copyable block, with no "Coming soon" note
