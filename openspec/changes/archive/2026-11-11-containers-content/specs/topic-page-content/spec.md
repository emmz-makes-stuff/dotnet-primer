## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. If one of them shows the placeholder, the site test gate SHALL fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection and Middleware pages and their three cheatsheets, the .NET 10 and `dotnet`
  CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages with their three
  cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its cheatsheet, the Testing
  page with its cheatsheet, the Security & auth page with its cheatsheet, the Async & performance
  page with its cheatsheet, and the Containers page with its cheatsheet

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

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

### Requirement: Containers page covers images and publishing without a Dockerfile
The Containers page SHALL describe the official .NET images (`sdk`, `aspnet`, `runtime` and
`runtime-deps`), their tags, and which one an app builds on and which it runs on. It SHALL show
building an image with `dotnet publish` and no Dockerfile, and setting the image's base, name, tag,
port and user through the project's container properties.

#### Scenario: Reader publishes an image without a Dockerfile
- **WHEN** a reader follows the page's `dotnet publish` example in an ASP.NET Core project
- **THEN** an image is built into their local Docker, and the page shows the command's real output
  and how to run the image

#### Scenario: Reader picks a base image
- **WHEN** a reader wants to know which image to build on and which to run on
- **THEN** the page says that the build uses `sdk`, that an ASP.NET Core app runs on `aspnet`, and
  when `runtime` or `runtime-deps` fits instead

### Requirement: Containers page covers multi-stage Dockerfiles
The Containers page SHALL show a multi-stage Dockerfile for an ASP.NET Core app, with a build stage
on the SDK image and a runtime stage without it. It SHALL explain layer caching, copying the project
file and restoring before the rest of the source, and keeping build output and secrets out of the
build context with `.dockerignore`.

#### Scenario: Reader builds the page's Dockerfile
- **WHEN** a reader builds the page's Dockerfile and runs the image
- **THEN** the app answers, and the final image holds no SDK

#### Scenario: Reader rebuilds after a code change
- **WHEN** a reader changes a source file and builds again
- **THEN** the page shows that the restore layer comes from the cache, as measured

### Requirement: Containers page covers non-root and smaller images
The Containers page SHALL say that .NET 8 and later images listen on port 8080 and provide a
non-root `app` user, and why an image shouldn't run as root. It SHALL compare the size of the same
app on the default, chiseled, chiseled-extra and Native AOT bases, with measured sizes, and say what
each leaves out and when it matters.

#### Scenario: Reader compares base images
- **WHEN** a reader reads the page's image-size comparison
- **THEN** the sizes are real measurements of one app, with the platform they were measured on, and
  each base has its trade-off stated

#### Scenario: Reader needs a shell in a chiseled container
- **WHEN** a reader wants to look inside a running container that has no shell
- **THEN** the page says why there's none, and how to inspect it anyway

### Requirement: Containers page covers configuration through the environment
The Containers page SHALL show configuring a containerized app with environment variables: the `__`
separator for nested settings, `ASPNETCORE_HTTP_PORTS` and `ASPNETCORE_ENVIRONMENT`, and that
environment variables override `appsettings.json`. It SHALL say that secrets don't belong in an
image, and link to the Security & auth page for where they go instead.

#### Scenario: Reader overrides a nested setting
- **WHEN** a reader runs the page's image with an environment variable for a nested setting
- **THEN** the app uses the variable's value, and the page shows the variable's name

### Requirement: Containers page covers Compose locally
The Containers page SHALL show running an app with the database it uses on a developer's machine,
using Docker Compose: a `compose.yaml` that builds or names the app's image, passes its settings as
environment variables, and starts the app only once the database is ready. It SHALL say how to stop
the services and remove their data.

#### Scenario: Reader starts an app and its database
- **WHEN** a reader runs the page's `compose.yaml` with `docker compose up`
- **THEN** SQL Server starts, the app starts after it and answers, and the page shows how to tell

#### Scenario: Apple silicon reader runs the database
- **WHEN** a reader on Apple silicon runs the page's `compose.yaml`
- **THEN** the page points to the Docker page's note on x64-only images

### Requirement: Containers page covers registries and ECR
The Containers page SHALL show tagging an image and pushing it to a registry, both with `docker`
and with `dotnet publish`, as verified against a local registry. It SHALL show the steps that are
specific to Amazon ECR (signing in, and the repository's address) from the AWS documentation, and
SHALL say that those steps weren't run for the page.

#### Scenario: Reader pushes to a local registry
- **WHEN** a reader follows the page's push example against a local registry container
- **THEN** the image is pushed, and pulling it back by its tag works

#### Scenario: Reader pushes to ECR
- **WHEN** a reader wants to push to Amazon ECR
- **THEN** the page gives the sign-in command and the repository address format, links to the AWS
  documentation they come from, and says they weren't run for the page

### Requirement: Containers page covers image scanning
The Containers page SHALL show scanning an image for packages with known vulnerabilities with
Trivy, run as a container so that nothing is installed. It SHALL show real results for the default
and chiseled images of the same app, and say that an image is patched by rebuilding it on an
updated base image.

#### Scenario: Reader scans an image
- **WHEN** a reader runs the page's Trivy command against an image they built
- **THEN** the page shows the real summary it printed, with the date it was run, since the results
  change as advisories are published

#### Scenario: Reader patches an image
- **WHEN** a scan reports a vulnerable package in the base image
- **THEN** the page says to pull the updated base and rebuild, not to patch the running container

### Requirement: Containers page maps .NET 3.5 habits
The Containers page SHALL have a "Coming from .NET 3.5" section, after its concepts and before its
Pitfalls. It SHALL map these habits: deploying to IIS by copying files or with an installer,
settings in `web.config` and `machine.config`, an app pool running under a privileged account, and
patching the server in place.

#### Scenario: Engineer looks for web.config
- **WHEN** an engineer used to `web.config` transforms reads the mapping
- **THEN** it points to `appsettings.json` and environment variables, and to the page's
  configuration section

#### Scenario: Engineer looks for server patching
- **WHEN** an engineer used to patching a server in place reads the mapping
- **THEN** it points to rebuilding the image on an updated base, and to the page's scanning section
