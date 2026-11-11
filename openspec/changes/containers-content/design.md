# Design

## Context

- **The stub** `deploy/containers.md` has a "What this covers" paragraph, two Learn links
  ("Containerize a .NET app with dotnet publish", ".NET container images") and the "Coming soon"
  note. It's in `EXPECTED_SLUGS` and the sidebar's Deploy group. Neither it nor a cheatsheet is in
  `FULL_CONTENT_SLUGS` or `CHEATSHEETS`. The Docker page already lists Containers among the pages
  that need Docker, so it doesn't change.
- **The conventions are set,** as for async-performance-content:
  - `site/README.md` gives the topic skeleton and the cheatsheet shape;
  - `samples/README.md` gives the exercise layout: `Starter`, `Solution`, and a shared `tests/`
    compiled into `Starter.Tests` and `Solution.Tests`, in both `.slnx` files;
  - xUnit v3 (ADR-0003), central package management, and warnings as errors from
    `samples/Directory.Build.props`.

  Exercise 07's `tests/SqlServerFixture.cs` is the model for 12's fixtures: an `AssemblyFixture`
  that turns `DockerUnavailableException` into a message that links to the Docker page. It uses
  `Testcontainers.MsSql` 4.16.0.
- **The build context needs thought.** The samples take `TargetFramework`, `Nullable` and
  `ImplicitUsings` from `samples/Directory.Build.props`, and package versions from
  `samples/Directory.Packages.props`. `global.json` at the repo root pins SDK 10.0.400
  (`latestFeature`). A Dockerfile whose context is the project folder sees none of those (D3).
- **Decisions Emmz made** (2026-11-11): see proposal.md. In short:
  - Containers only;
  - all four extras;
  - exercise 12 fixes both the SDK publish and a Dockerfile;
  - a local registry instead of a real ECR push;
  - Trivy, run as a container.
- **Port 5000 on this Mac is macOS ControlCenter's.** The probe's local registry listens on 5001,
  and so does the page's example.
- **The site gate goes red during the exercise section.** "Undescribed exercise is caught" fails as
  soon as `12-containers` exists, until the page links to it, as with 10 and 11 (D9).

## Goals / Non-Goals

**Goals:**
- An engineer finishes the page able to build a small, non-root image of an ASP.NET Core app, both
  with `dotnet publish` and with a Dockerfile, configure it from the environment, run it with its
  database locally, push it, and scan it.
- Every size, scan result and command output on the page is measured, with the platform and date.
- Exercise 12's tests check the images themselves, not the files that build them, and are
  deterministic on a shared CI runner.

**Non-Goals:**
- Orchestration: health checks, graceful shutdown, Kubernetes, Aspire. The Orchestration & AWS page
  owns them. The Compose example's database health check is the one exception (D6).
- Docker itself: installing it, and the `docker` CLI beyond what the page uses. The Docker page owns
  setup.
- Asserting image sizes or scan results in tests. They vary by platform and date.

## Decisions

### D1. Page shape and order

The topic skeleton, in this order:
1. The outcome, and the cheatsheet tip.
2. **The .NET images:** `sdk`, `aspnet`, `runtime`, `runtime-deps`; tags (`10.0`, a patch version,
   the `-noble-chiseled` variants, digests), and which to build on and which to run on.
3. **Publishing without a Dockerfile:** `dotnet publish -t:PublishContainer` (or
   `/t:PublishContainer`), the `Container…` properties (`ContainerRepository`,
   `ContainerImageTags`, `ContainerFamily`, `ContainerBaseImage`, `ContainerPort`, `ContainerUser`),
   running the image, and when a Dockerfile is the better choice.
4. **Multi-stage Dockerfiles:** the build and runtime stages, the restore layer, `.dockerignore`,
   and what a rebuild after a code change reuses (measured, D2).
5. **Non-root, port 8080, and smaller images:** the `app` user and `APP_UID`, why not root, the size
   table (D2), and what chiseled leaves out: no shell, no package manager, and no ICU or time zone
   data unless it's `-extra`. Native AOT's trade-offs. Inspecting a container with no shell
   (`docker debug`, if the probe finds Rancher Desktop supports it, or else `docker cp` and a
   sidecar container that shares its process namespace).
6. **Configuration through the environment:** `__`, `ASPNETCORE_HTTP_PORTS`,
   `ASPNETCORE_ENVIRONMENT`, the order of configuration sources, and that secrets don't go in the
   image (a link to Security & auth).
7. **Compose locally** (D6).
8. **Registries and ECR** (D7).
9. **Scanning** (D8).
10. **Coming from .NET 3.5:** the table.
11. **Pitfalls,** then **Exercise 12**.

The SDK publish comes before the Dockerfile because it's less to learn and gives a better image by
default. The page says when a Dockerfile is still needed: extra OS packages, a build that needs
more than `dotnet publish`, or a team that standardises on Dockerfiles.

### D2. Measured, not remembered

The probe (§1) measures everything the page states, on this Mac (`linux/arm64`, under Rancher
Desktop). It records the platform with each number:
- **image sizes** for one app on four bases: `aspnet:10.0`, `aspnet:10.0-noble-chiseled`,
  `aspnet:10.0-noble-chiseled-extra`, and Native AOT on `runtime-deps:10.0-noble-chiseled`. It also
  records what the default `10.0` tag's OS is, because the page shouldn't name one from memory;
- **the `dotnet publish` output,** and the image's default user and port (`docker inspect`);
- **the Dockerfile rebuild,** with only a `.cs` file changed: the restore step reported as
  `CACHED`;
- **the Compose run** (D6), the registry push and pull (D7), and the Trivy summaries (D8).

Native AOT for Linux can't be built on macOS, so the probe builds it in a Dockerfile on the
`sdk:10.0-noble-aot` image. If that tag doesn't exist for 10.0, the probe finds the documented way,
and the page uses that.

### D3. Exercise 12's build context: the project folder

**The exercise's app projects build in a container from their own folder,** as the page teaches
(`docker build .` next to the `.csproj`). So each app's `.csproj` states what a container build
can't get from up the tree:
- `TargetFramework`, `Nullable` and `ImplicitUsings`, the same values as `Directory.Build.props`;
- no `PackageReference`s: the app needs only the shared framework, so central package management
  has nothing to resolve.

The gates still build these projects with `Directory.Build.props`, so warnings stay errors. The
container build doesn't see `global.json` either, so the SDK image's own version builds it. That
isn't a risk, because the app uses nothing from 10.0.4xx.

Each app folder ships a `.dockerignore` (`bin/`, `obj/`), in both Starter and Solution. It isn't a
TODO, because without it a host's `obj/` would break the container's restore and fail every test
for a reason that has nothing to do with the exercise. The page explains it.

**Alternative considered:** `samples/` as the context, with the Dockerfile copying the props in
first. It matches the gates exactly, but it teaches a repo layout the engineers won't have, and
every `COPY` path gets longer. Rejected.

### D4. Exercise 12 shape

An orders API (`Containers.Starter`, `Containers.Solution`), with its orders in memory, like 11's.
It has `GET /orders`, `POST /orders`, and `GET /info`, which returns the warehouse name it was
configured with. At startup it logs its configured warehouse name, at Information.

The four bugs, and their TODOs:
1. **Program.cs, a hard-coded port:** `builder.WebHost.UseUrls("http://+:5000")`. The fix removes it,
   so the app takes `ASPNETCORE_HTTP_PORTS` (8080 in the .NET images).
2. **Program.cs, a setting the environment can't override:** the warehouse name is read through
   `new ConfigurationBuilder().AddJsonFile("appsettings.json")`, not the host's configuration. The
   fix binds it from `builder.Configuration` with options, so `Warehouse__Name` overrides it.
3. **The `.csproj`'s container properties:** `ContainerBaseImage` is the full `aspnet:10.0`, and
   `ContainerUser` is `root`. The fix uses the chiseled family (`ContainerFamily`), and drops the
   user so the image's non-root default applies.
4. **The Dockerfile:** a single stage on `sdk:10.0` that publishes and runs, as root. The fix is a
   build stage on `sdk:10.0` and a runtime stage on `aspnet:10.0-noble-chiseled`, running as
   `$APP_UID`, with the restore layer first.

Tags are major and minor (`10.0`), not pinned patches, so a rebuild picks up patched bases, which is
what the page teaches. A future base image changing behaviour is a risk (see Risks).

### D5. Testing the images, not the files

Two `AssemblyFixture`s, one per image, each built once for the whole run:
- **The SDK image:** `dotnet publish <app> -t:PublishContainer -c Release` run as a child process,
  with a repository name unique to Starter or Solution and to the run, so parallel runs and stale
  images don't collide.
- **The Dockerfile image:** Testcontainers' `ImageFromDockerfileBuilder`, with the app folder as
  the context.

Each test project gets its app's folder from an `AssemblyMetadata` item set in its `.csproj`. The
test projects don't reference the app projects, because they test images, not code.

The checks, each named with its TODO in its failure message:
- **Port (TODO 1):** start the container with 8080 exposed, wait for the log's `Now listening on`
  line, and check the address it names. Then check that `GET /info` answers on 8080. Reading the
  log means a wrong port fails at once, with the port named, rather than after a timeout.
- **Setting (TODO 2):** run with `Warehouse__Name` set, and read the startup log line. It doesn't
  depend on the port, so TODO 1 and TODO 2 fail apart.
- **Non-root (TODOs 3 and 4):** read `/proc/1/status` from the running container, through the
  Docker API's file copy, not a shell, because a chiseled image has none. `Uid` mustn't be 0.
- **Chiseled (TODO 3) and no SDK (TODO 4):** check that `/bin/sh` is absent, and for the Dockerfile
  image, that `/usr/share/dotnet/sdk` is too, through the same file API.

The probe (§1) confirms that the file API reads from a chiseled container, and fails cleanly when a
path is absent. **If it doesn't, stop and ask:** the fallback is reading the image's `Config.User`
and layer history through `docker inspect`, which checks the image's metadata rather than what runs.

Each fixture wraps `DockerUnavailableException` as 07 does, and the image build's output goes into
the failure message, so a broken Dockerfile shows its error.

### D6. Compose locally

The page's `compose.yaml` runs a small orders API with EF Core and SQL Server: written for the page,
compiled, and run in a scratch folder by the probe, not taken from exercise 07. The `db` service
uses exercise 07's pinned SQL Server image, with a `healthcheck` that runs `sqlcmd`. The `api` waits
on `depends_on: condition: service_healthy`, and gets its connection string as
`ConnectionStrings__Orders`. It ends with `docker compose down -v`.

The database health check is the one health check on this page. The page says that health checks
for the app itself are on the Orchestration & AWS page. For SQL Server on Apple silicon, it points
to the Docker page's note.

### D7. Registries and ECR

The probe runs `registry:2` on `localhost:5001`, then:
- `docker tag` and `docker push`, then `docker pull` by tag after removing the local copy;
- `dotnet publish -t:PublishContainer -p:ContainerRegistry=localhost:5001`, which pushes without
  the local daemon.

The ECR steps are `aws ecr get-login-password | docker login --username AWS --password-stdin
<account>.dkr.ecr.<region>.amazonaws.com`, and the repository address. They come from the AWS docs
(linked), the page says in a note that they weren't run for it, and they're syntax-checked only.

### D8. Scanning with Trivy

`docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:<version> image
<image>`, pinned to the version the probe finds current. The probe scans the default and chiseled
images of the same app, and the page shows each summary, by severity, with its date. Rancher Desktop
on macOS puts the socket at `~/.rd/docker.sock`, unless Administrative Access creates
`/var/run/docker.sock` (the Docker page). The page gives the mount for both.

`docs/dependencies.md` gets a Trivy row: pulled as an image on first use, nothing to install, and
it needs network access to download its vulnerability database. `make deps` gets no check, because
there's nothing to check until the first pull. `registry:2` is only used in the probe and on the
page, so it gets no row.

### D9. Section order

1. Probe and package version (D2, D5, D7, D8), and the Trivy row.
2. Exercise 12: scaffold, Solution, tests, Starter, CI.
3. The cheatsheet, then the Containers page.
4. The samples README.
5. The human check.

**§2 lands with `SITE_TEST_EXIT:1` on purpose:** that's the undescribed-exercise check for
`12-containers`. §3 turns it green. The section note and commit body record it.

### D10. Cheatsheet

`cheatsheets/containers`, linked both ways with the page:
- the images and when to use each;
- the SDK publish command and its `Container…` properties;
- a multi-stage Dockerfile and a `.dockerignore`;
- `docker` run, logs, inspect and exec, and inspecting a chiseled container;
- the environment variables;
- the Compose commands;
- tag, push, and ECR sign-in;
- the Trivy command.

It has Learn links, and links to the AWS ECR and Trivy docs.

### D11. Code accuracy and verification

As in earlier changes:
- every C# snippet is compiled in a scratch project, net10.0, with warnings as errors;
- every Dockerfile and `compose.yaml` on the page is built or run as shown;
- "from the exercise" blocks are copies of the real files;
- every URL on both pages is fetched.

## Risks / Trade-offs

- **The image tests are slow, and the first run pulls the base images.** → Each image is built once
  per run, by an assembly fixture. The section note records how long the gate takes, before and
  after.
- **Floating `10.0` tags can change under the tests.** A new base image could change the user, the
  port or the file layout. → The checks are the documented contract (8080, non-root, no shell), and
  a failure names what it found. It would be a real change worth knowing about.
- **CI builds `linux/amd64`, and this Mac builds `linux/arm64`.** → No test depends on the
  architecture. The page gives the platform with each size.
- **Trivy's results change daily.** → The page dates them, and talks about the difference between
  the bases rather than the exact counts.
- **The ECR steps aren't run.** → They're marked as such, linked to AWS's docs, and syntax-checked.
  The push itself is verified against a local registry.
- **`ContainerFamily` and tag names may differ from memory for .NET 10.** → The probe lists the
  real tags (`docker manifest inspect` or the MCR tag list) before anything is written.
