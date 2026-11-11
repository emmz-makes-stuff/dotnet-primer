---
title: "Containers"
description: "Packaging .NET apps as container images: the official images, dotnet publish without a Dockerfile, multi-stage Dockerfiles, non-root and chiseled images, configuration through the environment, Compose locally, pushing to a registry and ECR, and scanning with Trivy."
learn:
  - title: "Containerize a .NET app with dotnet publish"
    url: https://learn.microsoft.com/dotnet/core/containers/sdk-publish
  - title: "Container publishing properties"
    url: https://learn.microsoft.com/dotnet/core/containers/publish-configuration
  - title: ".NET container images"
    url: https://learn.microsoft.com/dotnet/core/docker/container-images
  - title: "Containerize a .NET app (with a Dockerfile)"
    url: https://learn.microsoft.com/dotnet/core/docker/build-container
  - title: "Native AOT deployment"
    url: https://learn.microsoft.com/dotnet/core/deploying/native-aot/
  - title: "Configuration in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
---

By the end of this page you'll be able to package an ASP.NET Core app as a container image, with `dotnet publish` alone or with a multi-stage Dockerfile. It runs as a non-root user, on a small base with little to patch, and takes its port and settings from the environment. You'll be able to run it with its database through Compose, push it to a registry, and scan it for known vulnerabilities.

:::tip[Cheatsheet]
Keep the [Containers cheatsheet](/cheatsheets/containers/) open while you work.
:::

The examples are the orders API from [exercise 12](#exercise-12-ship-the-orders-api-in-a-container), apart from the Compose example, which adds a database. Everything was measured on one machine: an Apple silicon Mac, so the images are `linux/arm64`, with SDK 10.0.404 and runtime 10.0.15, on 11 November 2026. You need [Docker](/platform/docker/) running.

## The .NET images

Microsoft publishes four images, at `mcr.microsoft.com/dotnet/<name>`. Each holds everything in the one below it:

| Image          | What's in it                                        | Use it to                              |
| -------------- | --------------------------------------------------- | -------------------------------------- |
| `sdk`          | The .NET SDK, and everything in `aspnet`            | build the app                          |
| `aspnet`       | The .NET and ASP.NET Core runtimes                  | run a web app                          |
| `runtime`      | The .NET runtime                                    | run a console app or a worker          |
| `runtime-deps` | Only the operating system libraries that .NET needs | run a self-contained or Native AOT app |

Build on `sdk` and run on `aspnet`. The SDK image is 976 MB, against 262 MB for `aspnet`, and none of the SDK is needed at run time. `runtime` fits a console app or a worker service with no web server. `runtime-deps` fits an app that brings its own runtime: one published self-contained, or compiled with [Native AOT](#native-aot).

The tag says which .NET and which operating system:

| Tag                         | Means                                                                                                      |
| --------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `10.0`                      | The latest 10.0 patch, on Ubuntu 24.04 (noble)                                                             |
| `10.0.15`                   | That patch only                                                                                            |
| `10.0-noble-chiseled`       | Ubuntu 24.04, chiseled: no shell, no package manager, no ICU (`aspnet`, `runtime` and `runtime-deps` only) |
| `10.0-noble-chiseled-extra` | Chiseled, with ICU and time zone data                                                                      |
| `10.0-resolute`             | Ubuntu 26.04, the newer choice. It has chiseled variants too                                               |
| `10.0-alpine`               | Alpine Linux                                                                                               |
| `sdk:10.0-noble-aot`        | An SDK image that can compile Native AOT                                                                   |

A tag like `10.0` moves: each patch release points it at a new image. That's what you want, because rebuilding your image then picks up the patched base. To make a build fully repeatable, pin the base by its digest instead (`aspnet:10.0@sha256:…`), and update the digest on purpose, often. A digest you never update is a base you never patch.

## Publishing without a Dockerfile

The SDK can build an image itself, with no Dockerfile and no SDK image. From the project's folder:

```sh
dotnet publish -t:PublishContainer -c Release -p:ContainerRepository=orders-api
```

```text
  Building image 'orders-api' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled'.
  Pushed image 'orders-api:latest' to local registry via 'docker'.
```

`-t:` and `/t:` are the same switch, so you'll see `/t:PublishContainer` too. "Local registry" means your own Docker. Without `ContainerRepository`, the image is named after the project, in lower case, and it's tagged `latest`. Run it:

```sh
docker run --rm --init -p 8080:8080 -e Warehouse__Name=Leeds orders-api
```

`--init` puts a small init process in front of the app, so a crash stops the container: see [Pitfalls](#pitfalls).

```text
info: Containers.Solution[514053180]
      Warehouse: Leeds
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
```

`GET http://localhost:8080/info` then answers `{"warehouse":"Leeds"}`. The app took both its port and its warehouse name from the environment: see [Configuration through the environment](#configuration-through-the-environment).

The image comes from properties in the `.csproj`, or on the command line with `-p:`:

```xml title="Containers.Solution.csproj (from the exercise)"
  <PropertyGroup>
    <ContainerFamily>noble-chiseled</ContainerFamily>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
```

| Property                                 | Sets                                            | Default                                                        |
| ---------------------------------------- | ----------------------------------------------- | -------------------------------------------------------------- |
| `ContainerRepository`                    | The image's name                                | The project's name, in lower case                              |
| `ContainerImageTags`                     | Its tags, separated by `;`                      | `latest`                                                       |
| `ContainerFamily`                        | The base's OS variant, such as `noble-chiseled` | The default OS                                                 |
| `ContainerBaseImage`                     | The whole base image, if you'd rather name it   | `aspnet:10.0` for a web app                                    |
| `ContainerPort` (an item)                | The ports the image says it listens on          | Worked out from the base's `ASPNETCORE_HTTP_PORTS`: `8080/tcp` |
| `ContainerUser`                          | The user the app runs as                        | `1654`, the base image's `app` user                            |
| `ContainerEnvironmentVariable` (an item) | A variable baked into the image                 | None                                                           |
| `ContainerRegistry`                      | Where to push                                   | Your local Docker                                              |

On the command line, tags need inner quotes, so the shell leaves the `;` alone: `-p:ContainerImageTags='"1.0.0;latest"'`.

**`ContainerFamily` and globalization.** With `ContainerFamily` set to `noble-chiseled`, the SDK picks `aspnet:10.0-noble-chiseled-extra`, not plain chiseled, unless the project sets `InvariantGlobalization` to `true`. Plain chiseled has no ICU, the library behind culture-aware formatting and sorting, so the SDK only uses it for an app that has said it doesn't need one:

| Properties                                                      | Base the SDK picked                |
| --------------------------------------------------------------- | ---------------------------------- |
| None                                                            | `aspnet:10.0`                      |
| `ContainerFamily=noble-chiseled`                                | `aspnet:10.0-noble-chiseled-extra` |
| `ContainerFamily=noble-chiseled`, `InvariantGlobalization=true` | `aspnet:10.0-noble-chiseled`       |

**When a Dockerfile is still the better choice:** the image needs extra OS packages; the build needs more than `dotnet publish`, such as a front end built with npm; or your team builds every image from a Dockerfile, and one more format isn't worth it.

## Multi-stage Dockerfiles

A Dockerfile can have several stages, each starting with `FROM`. The first builds the app on the SDK image. The last copies in only the published output, onto a runtime image, and that last stage is the image you ship:

```dockerfile title="Dockerfile (from the exercise)"
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The project file first, then restore: this layer comes from the cache until the project changes.
COPY Containers.Solution.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Containers.Solution.dll"]
```

```sh
docker build -t orders-api .
```

The image is 132 MB, and the SDK isn't in it: `/usr/share/dotnet/sdk` doesn't exist.

**Layer caching.** Each instruction makes a layer, and Docker reuses a layer when its instruction and its inputs haven't changed. So the project file is copied and restored on its own, before the rest of the source. Change a `.cs` file and build again, and the restore comes from the cache:

```text
#10 [build 4/6] RUN dotnet restore
#10 CACHED
#11 [build 5/6] COPY . .
#11 DONE 0.1s
#12 [build 6/6] RUN dotnet publish -c Release -o /app --no-restore
#12 DONE 1.7s
```

With `COPY . .` before the restore, every code change would restore every package again.

**`.dockerignore`.** `docker build .` sends the folder to Docker as the build context, and `COPY . .` copies all of it. Without a `.dockerignore`, that includes your machine's `bin/` and `obj/`: `obj/project.assets.json` holds paths from your machine, which break the restore in the container.

```text title=".dockerignore (from the exercise)"
# Your machine's build output stays out of the image: the container restores and builds its own.
# Without this, COPY . . brings in obj/project.assets.json, with paths from your machine.
bin/
obj/
```

List anything else that mustn't reach the image there too: a local `.env` file, or a key you use for testing. A file copied into a layer stays in the image, even if a later step deletes it.

## Non-root, port 8080 and smaller images

Since .NET 8, the images listen on port 8080, not 80, and include a non-root user called `app`, with UID 1654. `APP_UID` holds that number, for a Dockerfile's `USER $APP_UID`. A port below 1024 needs root, so 8080 lets the app run without it.

Why not root? A process that's root in a container is root on the files it can reach, and the container is the only thing between it and the host. If an attacker gets code running in your app, root makes every other weakness worse. As the `app` user, the app can't change its own files or install anything.

Who the app runs as depends on how you built the image:

| Image                                          | Runs as                  |
| ---------------------------------------------- | ------------------------ |
| `aspnet:10.0`, as it comes                     | **root**                 |
| `aspnet:10.0-noble-chiseled`, and `-extra`     | `1654`                   |
| Published by the SDK, on any base              | `1654` (the SDK sets it) |
| A Dockerfile `FROM aspnet:10.0` with no `USER` | **root**                 |

So a Dockerfile on the full `aspnet` image needs `USER $APP_UID`. On chiseled, the user is `app` already, but keep the line: it stays right if someone changes the base.

### Smaller images

The same app, a `dotnet new web` project, on each base. The size is `docker image ls`'s, for `linux/arm64`:

| Base                                              | Image size | What it leaves out                                                  |
| ------------------------------------------------- | ---------: | ------------------------------------------------------------------- |
| `aspnet:10.0` (Ubuntu 24.04)                      |     262 MB | Nothing: a shell, `apt`, and 97 OS packages                         |
| `aspnet:10.0-noble-chiseled-extra`                |     170 MB | The shell and the package manager. It has ICU and time zone data    |
| `aspnet:10.0-noble-chiseled`                      |     132 MB | Also ICU and time zone data: invariant globalization only           |
| Native AOT, on `runtime-deps:10.0-noble-chiseled` |    60.5 MB | Also the .NET runtime: the app is compiled to one native executable |

A chiseled image has the files .NET needs and nothing else: 8 OS packages, against 97. Fewer packages mean fewer to patch and fewer known vulnerabilities, as the [scan](#scanning-images) shows. The cost is that you can't open a shell in it, and an app that needs ICU or time zones has to use `-extra`. **An app that talks to SQL Server needs `-extra`:** on plain chiseled, SqlClient stops at startup with `System.NotSupportedException: Globalization Invariant Mode is not supported.`

### Native AOT

Native AOT compiles the app ahead of time to a native executable, with no JIT, so it starts faster and needs only `runtime-deps`. It has trade-offs:

- The build needs a native toolchain for the target, so a Linux image is built in a container on `sdk:10.0-noble-aot`, with `PublishAot` set to `true`. A Mac can't build one for Linux directly.
- No code is generated at run time, so libraries that rely on reflection may fail or warn: the build reports trimming and AOT warnings. Minimal APIs need source-generated JSON for their request and response types, and not every library supports AOT. Check Learn's [compatibility list](https://learn.microsoft.com/aspnet/core/fundamentals/native-aot) before you choose it.
- The build is slower, and you debug a native binary.

Use it when start-up time or image size matters, as for a function or something that scales from zero, and every library you use supports it. Otherwise, chiseled is most of the gain for none of the work.

### Looking inside a container with no shell

A chiseled image has no `sh`, so `docker exec orders sh` fails with `exec: "sh": executable file not found in $PATH`. That's the point: an attacker who gets in has no shell either. Look in from outside instead:

```sh
docker run -d --init --name orders -p 127.0.0.1:8080:8080 orders-api

docker logs orders                        # what the app wrote to the console
docker top orders                         # its processes, and the user each runs as
docker cp orders:/app/appsettings.json .  # copy a file out, to the current folder

# Borrow a shell: a BusyBox container that shares the app's processes.
docker run --rm --pid=container:orders busybox:1.37 ps
```

`docker top` shows the init process and the app, both as user 1654:

```text
PID                 USER                TIME                COMMAND
226824              1654                0:00                /sbin/docker-init -- dotnet /app/Containers.Solution.dll
226858              1654                0:00                dotnet /app/Containers.Solution.dll
```

The BusyBox container sees the same processes, numbered from inside the container, and its own `ps` as root:

```text
PID   USER     TIME  COMMAND
    1 1654      0:00 /sbin/docker-init -- dotnet /app/Containers.Solution.dll
    6 1654      0:00 dotnet /app/Containers.Solution.dll
   36 root      0:00 ps
```

From there, `cat /proc/6/status` and the rest of BusyBox's tools work on the app's process. Docker Desktop also has `docker debug`, which does this for you, but it isn't in the open-source `docker` CLI that Rancher Desktop uses.

## Configuration through the environment

In a container, settings arrive as environment variables. An ASP.NET Core app reads them already: the host's configuration reads `appsettings.json`, then `appsettings.{Environment}.json`, then environment variables, then the command line, and a later source overrides an earlier one. See [Configuration](/aspnet-core/configuration/) for the details.

A variable name can't contain `:`, so `__` (two underscores) stands for it. `Warehouse__Name` sets `Warehouse:Name`, which the exercise binds to its options:

```csharp title="Program.cs (from the exercise)"
builder.Services.AddOptions<WarehouseOptions>()
    .Bind(builder.Configuration.GetSection(WarehouseOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Run it with no variable, and it logs `Warehouse: Default warehouse`, from `appsettings.json`. With `-e Warehouse__Name=Leeds`, it logs `Warehouse: Leeds`. Read settings through `builder.Configuration`: a `ConfigurationBuilder` of your own that reads only the JSON file can't be overridden.

| Variable                    | Sets                                                           |
| --------------------------- | -------------------------------------------------------------- |
| `Warehouse__Name`           | `Warehouse:Name`                                               |
| `ConnectionStrings__Orders` | The connection string `GetConnectionString("Orders")` returns  |
| `ASPNETCORE_HTTP_PORTS`     | The ports the app listens on. The .NET images set it to `8080` |
| `ASPNETCORE_ENVIRONMENT`    | The environment's name. `Production` unless it's set           |

So don't set the port in code. `UseUrls` or a URL in `appsettings.json` overrides `ASPNETCORE_HTTP_PORTS`, and then whoever runs the container can't change it. To listen on another port, set the variable: `-e ASPNETCORE_HTTP_PORTS=9090`.

**Secrets don't go in the image.** Anything in an image, whether in `appsettings.json`, an `ENV` line or a layer that a later step deleted, can be read by anyone who can pull it. Pass secrets in at run time, from your platform's secrets store, as the [Secrets](/security/#secrets) section of Security & auth describes.

## Compose locally

An app rarely runs alone. Docker Compose starts it and what it needs, here SQL Server, from one file. This example is a small orders API on EF Core, written for this page:

```csharp title="Program.cs"
// From ConnectionStrings__Orders, which compose.yaml sets.
builder.Services.AddDbContext<OrdersDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")));
```

Its Dockerfile is the one above, with `-noble-chiseled-extra` as the runtime base, because SqlClient needs ICU. The `compose.yaml` sits next to the API's folder:

```yaml title="compose.yaml"
services:
  db:
    image: mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${DB_PASSWORD:?Set DB_PASSWORD in .env}"
    volumes:
      - db-data:/var/opt/mssql
    healthcheck:
      # $$ passes a $ through to the container's shell, which reads the variable.
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$${MSSQL_SA_PASSWORD}" -C -Q "SELECT 1" -b
      interval: 5s
      timeout: 5s
      retries: 20
      start_period: 10s

  api:
    build: ./Orders.Api
    init: true
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__Orders: "Server=db;Database=Orders;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=true"
    depends_on:
      db:
        condition: service_healthy

volumes:
  db-data:
```

Compose reads `${DB_PASSWORD}` from a `.env` file next to it, which stays out of source control:

```text title=".env"
DB_PASSWORD=Choose-A-Strong-Password-1
```

The API reaches the database by its service name, `db`. The `healthcheck` runs a query until SQL Server answers, and `depends_on` with `condition: service_healthy` holds the API back until then. Without it, the API starts first and fails to connect. The health check here is the database's: health checks for the app itself are on the [Orchestration & AWS](/deploy/orchestration-and-aws/) page. `init: true` runs a small init process as PID 1, with the app under it, so that if the app crashes, its container stops: see [Pitfalls](#pitfalls).

```sh
docker compose up -d --build
```

```text
 Container cmp-db-1 Started
 Container cmp-db-1 Waiting
 Container cmp-db-1 Healthy
 Container cmp-api-1 Starting
 Container cmp-api-1 Started
```

The container names come from the folder, here `cmp`. `Healthy`, and then the API starting, is how you tell it worked, and `docker compose ps` shows `(healthy)` in the database's status. Then `POST /orders` with `{"sku":"SKU-1"}` answers `201`, and `GET /orders` answers `[{"id":1,"sku":"SKU-1"}]`. `docker compose logs -f api` follows the API's log.

**On Apple silicon,** the SQL Server image is `linux/amd64` only. Compose warns that the platform doesn't match, and the database runs under emulation: see the [Docker page's note](/platform/docker/#macos) on images for x64 only.

**If port 8080 is taken** on your machine, change the host's side of the mapping, `"5080:8080"`, and use `localhost:5080`. The container's side stays 8080.

To stop, run `docker compose down`. That removes the containers and keeps the database's volume, so the data is still there next time. `docker compose down -v` removes the volume, and the data with it:

```text
 Container cmp-db-1 Removed
 Volume cmp_db-data Removed
 Network cmp_default Removed
```

## Registries and ECR

A registry stores images, so a server or a cluster can pull them. You tag an image with the registry's address, then push it. To try it, run a registry locally (port 5000 is often taken on a Mac, so this uses 5001):

```sh
docker run -d --name registry -p 5001:5000 registry:3

docker tag orders-api localhost:5001/orders-api:1.0.0
docker push localhost:5001/orders-api:1.0.0
```

```text
1.0.0: digest: sha256:4f88c3eeb440fd5f4d3cbd5cc8b10afc5fc4fcc933e42fd3ab569d66bee2a8e9 size: 1575
```

After `docker image rm localhost:5001/orders-api:1.0.0`, `docker pull localhost:5001/orders-api:1.0.0` brings it back: `Status: Downloaded newer image for localhost:5001/orders-api:1.0.0`.

The SDK can push straight to the registry, without your local Docker:

```sh
dotnet publish -t:PublishContainer -c Release -p:ContainerRepository=orders-api -p:ContainerRegistry=localhost:5001
```

```text
  Pushed image 'orders-api:latest' to registry 'localhost:5001'.
```

Tag each image you ship with a version, such as `1.0.0` or the commit's hash, and deploy that tag. `latest` is only the most recent push, so two servers that pull it at different times can run different code, and you can't tell which version a container runs or go back to the one before.

### Amazon ECR

ECR is AWS's registry. Its address is `<account>.dkr.ecr.<region>.amazonaws.com`, with one repository for each image. These steps are from the AWS docs, [Pushing a Docker image to an Amazon ECR private repository](https://docs.aws.amazon.com/AmazonECR/latest/userguide/docker-push-ecr-image.html) and [Private registry authentication](https://docs.aws.amazon.com/AmazonECR/latest/userguide/registry_auth.html).

:::note[Not run for this page]
These commands weren't run against ECR for this page. They use example values: the account `123456789012` and the region `eu-west-2`.
:::

```sh
# Sign Docker in to the registry. The sign-in lasts 12 hours.
aws ecr get-login-password --region eu-west-2 | docker login --username AWS --password-stdin 123456789012.dkr.ecr.eu-west-2.amazonaws.com

docker tag orders-api 123456789012.dkr.ecr.eu-west-2.amazonaws.com/orders-api:1.0.0
docker push 123456789012.dkr.ecr.eu-west-2.amazonaws.com/orders-api:1.0.0
```

The repository has to exist before the push: create it with `aws ecr create-repository --repository-name orders-api`, or in Terraform. With `dotnet publish`, sign in the same way, then set `ContainerRegistry` to the registry's address. The SDK uses the credentials that `docker login` stored.

## Scanning images

An image holds OS packages, and the .NET runtime, and your app's NuGet packages, and any of them can have a known vulnerability. [Trivy](https://trivy.dev/) checks an image against a database of published advisories. Run it as a container, so there's nothing to install:

```sh
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock -v trivy-cache:/root/.cache \
  aquasec/trivy:0.76.2 image --scanners vuln orders-api
```

The first mount lets Trivy read your images through Docker. The `trivy-cache` volume keeps its vulnerability database between runs, rather than downloading it every time. On Rancher Desktop for macOS, `/var/run/docker.sock` exists only with Administrative Access turned on: see the [Docker page's socket note](/platform/docker/#macos).

The same app, scanned on 11 November 2026, with the database updated at 13:00 UTC that day. On `aspnet:10.0`:

```text
orders-api-full (ubuntu 24.04)
==============================
Total: 22 (UNKNOWN: 0, LOW: 5, MEDIUM: 17, HIGH: 0, CRITICAL: 0)
```

And on `aspnet:10.0-noble-chiseled`, the OS line of Trivy's summary:

```text
│ orders-api (ubuntu 24.04)                                                        │   ubuntu    │        0        │
```

Both found nothing in the .NET runtime or the app's dependencies. The 22 are in packages that chiseled leaves out, such as `util-linux`, `dash`, `gnutls` and `shadow`. Your numbers will differ, because new advisories are published every day.

In CI, make the scan a gate: `--severity HIGH,CRITICAL --exit-code 1` fails the step when there's anything high or critical.

**To patch an image, rebuild it.** Pull the updated base and rebuild (`docker build --pull`, or `dotnet publish` again: the SDK reads the base from the registry, not from your local Docker), then redeploy. Don't patch a running container: the next one started from the image has the old packages again. With a moving tag like `10.0`, a rebuild is all it takes, so rebuild on a schedule, not only when you change the code.

## Coming from .NET 3.5

| In .NET 3.5                                                         | Now                                                                                                                                        |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Deploying to IIS by copying files, Web Deploy, or an MSI installer  | An image, built once by [`dotnet publish`](#publishing-without-a-dockerfile) or a [Dockerfile](#multi-stage-dockerfiles), and run anywhere |
| Settings in `web.config`, with a transform for each environment     | `appsettings.json`, overridden by [environment variables](#configuration-through-the-environment) where it runs                            |
| `machine.config` settings shared by every app on the server         | Nothing shared: each image carries its own runtime and settings                                                                            |
| An app pool running as `LocalSystem`, or another privileged account | The non-root [`app` user](#non-root-port-8080-and-smaller-images)                                                                          |
| Patching the server in place with Windows Update                    | [Rebuilding the image](#scanning-images) on an updated base, and redeploying                                                               |
| Remote Desktop onto the server to look around                       | [`docker logs`, `docker top` and `docker cp`](#looking-inside-a-container-with-no-shell)                                                   |
| A connection string in `web.config`, encrypted with `aspnet_regiis` | `ConnectionStrings__Orders` from the environment, and a [secrets store](/security/#secrets)                                                |

## Pitfalls

- **Running as root.** The full `aspnet` image and a Dockerfile with no `USER` run the app as root, and `ContainerUser=root` does it on purpose. Fix: `USER $APP_UID` in a Dockerfile, or no `ContainerUser` with the SDK, and check with `docker top`.
- **Shipping the SDK in the runtime image.** A single-stage Dockerfile on `sdk:10.0` is the exercise's Starter: 977 MB, with the compiler, a shell and root, against 132 MB for the two-stage Solution. Fix: build in one stage, run in another.
- **No `.dockerignore`.** `COPY . .` brings in your machine's `bin/` and `obj/`, and the restore in the container fails or uses the wrong paths. Fix: a `.dockerignore` with `bin/` and `obj/`, next to the Dockerfile.
- **The source copied before the restore.** Every code change invalidates the restore layer, so every build downloads every package again. Fix: copy the `.csproj`, restore, then copy the rest.
- **A port or a setting fixed in code.** `UseUrls("http://+:5000")` ignores `ASPNETCORE_HTTP_PORTS`, so the container listens where nobody expects. A setting read by your own `ConfigurationBuilder` ignores the environment. Fix: no `UseUrls`, and read everything through `builder.Configuration`.
- **A secret baked into an image layer.** A password in `appsettings.json`, an `ENV` line, or a file that a later step deletes is still in the image for anyone who pulls it. Fix: pass secrets in at run time, and keep files like `.env` in `.dockerignore`.
- **A pinned base image that's never rebuilt.** An image built once on `aspnet:10.0.15`, or a digest, keeps that patch's vulnerabilities for as long as it runs. Fix: rebuild on a schedule, and move pinned tags and digests forward on purpose.
- **`latest` in production.** You can't tell which version is running, two servers can run different ones, and rolling back means rebuilding. Fix: deploy a version tag, or a digest.
- **Plain chiseled for an app that needs ICU.** SqlClient fails at startup with "Globalization Invariant Mode is not supported". Fix: `-noble-chiseled-extra`, or `ContainerFamily` without `InvariantGlobalization`, which picks it.
- **A crashed app whose container keeps running.** The app is the container's first process, PID 1, and Linux ignores the abort signal that ends a .NET process after an unhandled exception, if it's sent to PID 1. So the app logs `Unhandled exception.`, then spins at full CPU, and the container stays `running`, with exit code 0. Nothing restarts it, because nothing sees it fail. That was measured here on a chiseled image, as the non-root user. Fix: run the container with an init process, with `docker run --init` or `init: true` in Compose. The container then exits with code 134. On an orchestrator, a health check also catches it.
- **`localhost:8080` reaches something else.** Another process on your machine may hold the port. Fix: map a free host port, such as `-p 5080:8080`, rather than stopping whatever has it.

## Exercise 12: ship the orders API in a container

**Goal:** make an orders API fit to run in a container, then build it into an image two ways: with `dotnet publish`, and with a Dockerfile. The app has a hard-coded port and a setting that the environment can't override. Its container properties build on the full `aspnet` image as root, and its Dockerfile ships the SDK. The endpoints, the order store and the validation are already written.

**Where:** [`samples/exercises/12-containers`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/12-containers) in the GitHub repo. Clone the repo, then work in the `Starter` project. **The tests need [Docker](/platform/docker/) running**: they build both images and run them, so the first run downloads the .NET base images.

**Steps:**

1. In `Starter/Program.cs`, remove the line that pins the app to port 5000, so it listens where `ASPNETCORE_HTTP_PORTS` says (TODO 1).
2. In the same file, bind `WarehouseOptions` from `builder.Configuration`, not from a `ConfigurationBuilder` of its own, so `Warehouse__Name` overrides `appsettings.json` (TODO 2).
3. In `Starter/Containers.Starter.csproj`, build on the chiseled family with `ContainerFamily`, set `InvariantGlobalization`, and remove `ContainerUser`, so the SDK's image runs as the non-root `app` user (TODO 3).
4. Split `Starter/Dockerfile` into a build stage on `sdk:10.0` that restores before copying the source, and a runtime stage on `aspnet:10.0-noble-chiseled` that runs as `$APP_UID` (TODO 4).

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/12-containers/Starter.Tests
```

The checks run against both images, and the SDK check against the Dockerfile's only. Each failure names the TODO to look at. They check what the running container does, not what the files say: the port it listens on, the warehouse name it logs, the user in `docker top`, and that `/bin/sh` and the SDK aren't there. If an image doesn't build, the failure shows what `dotnet publish` or Docker said.

**Try it yourself:** from `samples/exercises/12-containers/Starter`, run `docker build -t orders-starter .` and `docker image ls orders-starter` before and after TODO 4, to see the SDK leave the image.

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
