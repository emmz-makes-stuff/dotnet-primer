# Proposal

## Why

Containers is the next placeholder on the learning path, after Async & performance. Emmz chose it as
the next change on 2026-11-11. The engineers come from .NET 3.5, where an app was deployed to IIS on
a Windows server that someone patched and configured by hand, with settings in `web.config` and
`machine.config`. Their .NET 10 services will ship as Linux container images and run on AWS, so they
need to build an image that's small, runs as a non-root user, takes its configuration from the
environment, can be pushed to a registry and is checked for vulnerable packages. "Containers" is
named in DISCOVERY.md's problem statement, beside .NET 10, ASP.NET Core, EF Core and gRPC.

## What Changes

- **Write the full Containers page** (`deploy/containers`), replacing the stub. It follows the
  stub's outline, plus the four extras Emmz picked on 2026-11-11:
  - **The official .NET images:** `sdk`, `aspnet`, `runtime` and `runtime-deps`, their tags, and
    which to build with and which to run on.
  - **`dotnet publish` without a Dockerfile:** the SDK's container support, and its
    `Container…` properties for the base image, the repository and tag, the port and the user.
  - **Multi-stage Dockerfiles:** a build stage on `sdk` and a runtime stage on `aspnet`, a restore
    layer that's cached, and `.dockerignore`.
  - **Non-root and chiseled images:** the `app` user and port 8080 in .NET 8+ images, and why not
    to run as root.
  - **Configuration through environment variables:** the `__` separator, `ASPNETCORE_HTTP_PORTS`,
    `ASPNETCORE_ENVIRONMENT`, and keeping secrets out of the image.
  - **Extra: smaller images.** Measured sizes for the default, chiseled, chiseled-extra and Native
    AOT bases, and what each leaves out.
  - **Extra: Docker Compose locally.** An app and the database it uses (SQL Server), with
    `compose.yaml`, environment settings, and waiting until the database is ready.
  - **Extra: pushing to a registry.** Tag and push, verified against a local registry container.
    The Amazon ECR steps come from the AWS docs, and the page says they weren't run for it (Emmz,
    2026-11-11).
  - **Extra: image scanning** with Trivy, run as a container (Emmz, 2026-11-11), and rebuilding on
    a patched base image.
  - **Coming from .NET 3.5:** a short table, after the concepts and before the Pitfalls.
  - **Pitfalls,** then **Exercise 12**.
- **A Containers cheatsheet** (`cheatsheets/containers`). It's linked both ways with the page,
  listed in the sidebar and on the introduction, and added to the site tests' maps.
- **Exercise 12** (`samples/exercises/12-containers`): a starter, a solution and shared tests, like
  the earlier exercises. The engineer gets a working orders API whose container setup is wrong, and
  fixes it **twice: once with the SDK's publish properties, once in a multi-stage Dockerfile**
  (Emmz, 2026-11-11). The bugs:
  - a port hard-coded in the app;
  - a setting that an environment variable can't override;
  - an image that runs as root;
  - a fat image: the full runtime base for the SDK publish, and a single `sdk` stage in the
    Dockerfile.

  **The shared tests build each image and run it in Docker**, with Testcontainers, and catch each
  bug with a message that names its TODO. Like exercise 07's, these tests need Docker.
- **The samples README:** the layout and the exercise table gain 12, and the Docker prerequisite
  names it as well as 07.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" adds the Containers page and its cheatsheet.
  - New: "Containers page covers images and publishing without a Dockerfile", "… covers
    multi-stage Dockerfiles", "… covers non-root and smaller images", "… covers configuration
    through the environment", "… covers Compose locally", "… covers registries and ECR",
    "… covers image scanning", and "Containers page maps .NET 3.5 habits".
- **`site-structure`:** "Cheatsheets per area" grows from twelve cheatsheets to thirteen.
- **`sample-exercises`:**
  - "Site describes each exercise" gains a scenario for exercise 12.
  - New: "Containers exercise fixes the app for containers", "Containers exercise fixes the
    SDK-published image", and "Containers exercise fixes the Dockerfile".

## Impact

- **Site:**
  - `site/src/content/docs/deploy/containers.md`, and a new
    `site/src/content/docs/cheatsheets/containers.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`).
- **Samples:**
  - `samples/exercises/12-containers`, and both `.slnx` files;
  - a `Testcontainers` `PackageVersion` in `samples/Directory.Packages.props`, at the version
    `Testcontainers.MsSql` already uses;
  - `samples/README.md`.
- **Gates:** `make samples-test` builds two container images and runs them, so it takes longer.
  The first run pulls the .NET base images. Docker is already required (exercise 07), so nothing
  new is installed.
- **Tooling:** Trivy runs as a container image (`aquasec/trivy`), so there's nothing to install.
  It gets a row in `docs/dependencies.md` saying so. The local registry (`registry:2`) is only used
  in the probe, so it gets no row. No AWS CLI or login is added.
- **Out of scope:**
  - orchestration and AWS hosting: .NET Aspire, health checks and graceful shutdown, Kubernetes,
    ECS, EKS and Lambda. They belong to the Orchestration & AWS page, which stays a placeholder;
  - a real push to Amazon ECR, and publishing images from CI;
  - Windows containers;
  - scanners other than Trivy, beyond naming them.
