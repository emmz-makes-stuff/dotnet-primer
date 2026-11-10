## ADDED Requirements

### Requirement: Getting started page explains how to get Docker
The Getting started page SHALL have a section on getting Docker. It SHALL say which primer pages need
Docker and that exercises 01–06 don't, and SHALL give a recipe for each of Windows, macOS and Linux.
Each recipe SHALL be a path whose software is free to use at work, or SHALL say which licence the
software needs.

#### Scenario: Reader checks whether they need Docker yet
- **WHEN** a reader starting the primer reads the section
- **THEN** it names the pages that use Docker, and says that exercises 01–06 run without it

#### Scenario: Reader at a large company picks a tool
- **WHEN** a reader whose employer has no Docker Desktop or OrbStack subscription reads the recipe
  for their platform
- **THEN** it gives them a path whose software is free to use at work, and says which of the
  alternatives need a paid licence

### Requirement: Windows readers get Docker Engine in WSL 2, reachable from Windows
The Windows recipe SHALL install Docker Engine inside a WSL 2 Linux distro, start it automatically,
and make it reachable from Windows-side .NET through `DOCKER_HOST`. The TCP endpoint SHALL listen on
loopback only, and the page SHALL say that it has no authentication. Docker Desktop with its WSL 2
backend SHALL be named as the alternative.

#### Scenario: Reader runs the dashboard from WSL
- **WHEN** a Windows reader who followed the recipe runs the Logging page's dashboard `docker run`
  command in their WSL terminal
- **THEN** the dashboard opens in their Windows browser at `http://localhost:18888`, and an app run
  on Windows with `dotnet run` sends it telemetry on `localhost:4317`

#### Scenario: Windows-side .NET reaches the engine
- **WHEN** a Windows reader who followed the recipe opens a new Windows terminal
- **THEN** `DOCKER_HOST` points at `tcp://localhost:2375`, and the page's Windows check shows that
  the engine in WSL answers on it

#### Scenario: Engine starts with WSL
- **WHEN** a Windows reader restarts WSL (`wsl --shutdown`) and opens their distro again
- **THEN** the Docker daemon is running without a manual start

### Requirement: The Docker section ends with a check
The section SHALL end with a check that a container runs and that the daemon answers, with the
output to expect. The page's final "Check your setup" recipe SHALL stay unchanged, because exercises
01–06 don't need Docker.

#### Scenario: Reader checks their Docker setup
- **WHEN** a reader runs the section's check after following their platform's recipe
- **THEN** `docker run --rm hello-world` prints Docker's "Hello from Docker!" message, and
  `docker info --format '{{.ServerVersion}}'` prints a version number

#### Scenario: Reader whose daemon isn't running
- **WHEN** the check fails because the daemon isn't running, or because the user isn't yet in the
  `docker` group
- **THEN** the page's Pitfalls list names that error and how to fix it
