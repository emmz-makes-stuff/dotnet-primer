## ADDED Requirements

### Requirement: Docker page explains how to get Docker
The site SHALL have a Docker page that follows the topic page structure. It SHALL say which primer
pages need Docker and that exercises 01–06 don't, and SHALL give a recipe for each of Windows, macOS
and Linux. Each recipe SHALL be a path whose software is free to use at work, or SHALL say which
licence the software needs. The .NET 10 and `dotnet` CLI page SHALL link to it.

#### Scenario: Reader checks whether they need Docker yet
- **WHEN** a reader starting the primer reads the Docker page
- **THEN** it names the pages that use Docker, and says that exercises 01–06 run without it

#### Scenario: Reader at a large company picks a tool
- **WHEN** a reader whose employer has no Docker Desktop or OrbStack subscription reads the recipe
  for their platform
- **THEN** it gives them a path whose software is free to use at work, and says which of the
  alternatives need a paid licence

### Requirement: Windows and macOS readers get Rancher Desktop by default
The Windows and macOS recipes SHALL install Rancher Desktop with its dockerd (moby) engine and with
Kubernetes turned off, so `docker` works from the reader's usual terminal. They SHALL say how
Testcontainers finds the engine. Each SHALL name its alternatives with their licence terms: Docker
Desktop on both, and OrbStack and Colima on macOS.

#### Scenario: Windows reader uses docker from PowerShell
- **WHEN** a Windows reader who followed the recipe opens a new PowerShell
- **THEN** `docker run --rm hello-world` works there, and `DOCKER_HOST` names Rancher Desktop's
  `npipe:////./pipe/docker_engine` for Windows-side tools such as Testcontainers

#### Scenario: Reader runs the dashboard
- **WHEN** a Windows or macOS reader who followed the recipe runs the Logging page's dashboard
  `docker run` command
- **THEN** the dashboard opens in their browser at `http://localhost:18888`, and an app run with
  `dotnet run` sends it telemetry on `localhost:4317`

#### Scenario: Reader picked the wrong engine
- **WHEN** a reader chose containerd at Rancher Desktop's first run, and `docker` isn't found
- **THEN** the page's Pitfalls section says to switch to dockerd (moby) in Rancher Desktop's
  preferences

### Requirement: Advanced Windows path without a desktop app
The Windows recipe SHALL offer, collapsed and labelled advanced, Docker Engine inside a WSL 2 distro
that starts automatically and that Windows-side tools reach through `DOCKER_HOST` over TCP. The TCP
endpoint SHALL listen on loopback only, and the page SHALL say that it has no authentication.

#### Scenario: Advanced reader's Windows tools reach the engine
- **WHEN** a Windows reader who followed the advanced steps opens a new PowerShell
- **THEN** `DOCKER_HOST` points at `tcp://localhost:2375`, and the page's check for that path shows
  that the engine in WSL answers on it

#### Scenario: Engine starts with WSL
- **WHEN** a reader on the advanced path restarts WSL (`wsl --shutdown`) and opens their distro again
- **THEN** the Docker daemon is running without a manual start

### Requirement: Untested steps say so and invite reports
Where the page's steps have not been run end to end on a platform, the page SHALL say so near the
top of that platform's part, and link a new GitHub issue prefilled with a title, the `content`
label, and prompts for the reader's OS and tool versions, the failed step and its output.

#### Scenario: Reader whose step fails
- **WHEN** a step of the Windows or macOS recipe fails for a reader
- **THEN** a note near the top of that part says the steps haven't been tested end to end there, and
  its link opens the prefilled issue

### Requirement: The Docker page ends its setup with a check
The Docker page SHALL end its setup steps with a check that a container runs and that the daemon
answers, with the output to expect. The .NET 10 and `dotnet` CLI page's "Check your setup" recipe
SHALL stay unchanged, because exercises 01–06 don't need Docker.

#### Scenario: Reader checks their Docker setup
- **WHEN** a reader runs the page's check after following their platform's recipe
- **THEN** `docker run --rm hello-world` prints Docker's "Hello from Docker!" message, and
  `docker info --format '{{.ServerVersion}}'` prints a version number

#### Scenario: Reader whose daemon isn't running
- **WHEN** the check fails because the daemon isn't running, or because the user isn't yet in the
  `docker` group
- **THEN** the Docker page's Pitfalls section names that error and how to fix it
