---
title: Containers cheatsheet
description: The .NET images, dotnet publish container properties, a multi-stage Dockerfile, docker commands for a container with no shell, environment variables, Compose, pushing to a registry and ECR, and scanning with Trivy, on one page.
prev: false
next: false
learn:
  - title: Containerize a .NET app with dotnet publish
    url: https://learn.microsoft.com/dotnet/core/containers/sdk-publish
  - title: .NET container images
    url: https://learn.microsoft.com/dotnet/core/docker/container-images
  - title: Containerize a .NET app (with a Dockerfile)
    url: https://learn.microsoft.com/dotnet/core/docker/build-container
  - title: Configuration in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
---

Back to [Containers](/deploy/containers/).

## The images

All are `mcr.microsoft.com/dotnet/<name>:<tag>`.

| Image          | What's in it                       | Use it to                              |
| -------------- | ---------------------------------- | -------------------------------------- |
| `sdk`          | The SDK, and everything below      | build (the first stage)                |
| `aspnet`       | The .NET and ASP.NET Core runtimes | run a web app                          |
| `runtime`      | The .NET runtime                   | run a console app or worker            |
| `runtime-deps` | Only the OS libraries .NET needs   | run a self-contained or Native AOT app |

| Tag                         | Means                                                        |
| --------------------------- | ------------------------------------------------------------ |
| `10.0`                      | Latest 10.0 patch, on Ubuntu 24.04 (noble)                   |
| `10.0.15`                   | That patch, until you change the tag                         |
| `10.0-noble-chiseled`       | Ubuntu 24.04, chiseled: no shell, no package manager, no ICU |
| `10.0-noble-chiseled-extra` | Chiseled, plus ICU and time zone data                        |
| `10.0-resolute-chiseled`    | The same, on Ubuntu 26.04                                    |
| `sdk:10.0-noble-aot`        | An SDK image that can build Native AOT                       |

## Publish without a Dockerfile

```bash
# Builds the image into your local Docker. Named after the project, tagged latest.
dotnet publish -t:PublishContainer -c Release

# Choose the name and tags on the command line (the inner quotes keep the ; from the shell).
dotnet publish -t:PublishContainer -c Release -p:ContainerRepository=orders-api -p:ContainerImageTags='"1.0.0;latest"'
```

```xml title="Orders.Api.csproj"
<PropertyGroup>
  <ContainerRepository>orders-api</ContainerRepository>
  <ContainerImageTags>1.0.0;latest</ContainerImageTags>

  <!-- Chiseled base. Without InvariantGlobalization, the SDK picks -noble-chiseled-extra. -->
  <ContainerFamily>noble-chiseled</ContainerFamily>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>

<ItemGroup>
  <ContainerEnvironmentVariable Include="Warehouse__Name" Value="Leeds" />
</ItemGroup>
```

| Property or item     | Default                                             |
| -------------------- | --------------------------------------------------- |
| `ContainerBaseImage` | `aspnet:10.0` for a web app                         |
| `ContainerFamily`    | none (the default OS)                               |
| `ContainerUser`      | `1654`, the image's `app` user                      |
| `ContainerPort` item | `8080/tcp`, from the base's `ASPNETCORE_HTTP_PORTS` |
| `ContainerRegistry`  | none: your local Docker                             |

## A multi-stage Dockerfile

```dockerfile title="Dockerfile"
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Orders.Api.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
```

```text title=".dockerignore"
bin/
obj/
```

```bash
docker build -t orders-api .
```

## Run and look inside

```bash
docker run --rm -p 8080:8080 orders-api
docker run -d --name orders -p 127.0.0.1:8080:8080 -e Warehouse__Name=Leeds orders-api

docker logs -f orders
docker top orders                       # the processes, and the user each runs as
docker inspect orders-api --format '{{.Config.User}} {{json .Config.ExposedPorts}}'
docker cp orders:/app/appsettings.json .    # copy a file out, to the current folder

# A chiseled container has no shell, so `docker exec orders sh` fails.
# Borrow one: a sidecar that shares its processes.
docker run --rm --pid=container:orders busybox:1.37 cat /proc/1/status

docker rm -f orders
```

## Configuration from the environment

| Variable                    | Sets                                          |
| --------------------------- | --------------------------------------------- |
| `Warehouse__Name=Leeds`     | `Warehouse:Name`: `__` stands for `:`         |
| `ConnectionStrings__Orders` | `GetConnectionString("Orders")`               |
| `ASPNETCORE_HTTP_PORTS`     | The ports to listen on; the images set `8080` |
| `ASPNETCORE_ENVIRONMENT`    | `Production` unless set                       |

Environment variables override `appsettings.json` and `appsettings.{Environment}.json`. Secrets come from the environment or a secrets store at run time, never from the image.

## Compose

```yaml title="compose.yaml"
services:
  db:
    image: mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${DB_PASSWORD:?Set DB_PASSWORD in .env}"
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$${MSSQL_SA_PASSWORD}" -C -Q "SELECT 1" -b
      interval: 5s
      retries: 20

  api:
    build: ./Orders.Api
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__Orders: "Server=db;Database=Orders;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=true"
    depends_on:
      db:
        condition: service_healthy
```

```bash
docker compose up -d --build     # build, then start db, wait until it's healthy, start api
docker compose ps
docker compose logs -f api
docker compose down              # stop and remove the containers
docker compose down -v           # ... and the volumes: the database's data too
```

## Push to a registry

```bash
# A local registry, to try it out (port 5000 is often taken on a Mac).
docker run -d --name registry -p 5001:5000 registry:3

docker tag orders-api localhost:5001/orders-api:1.0.0
docker push localhost:5001/orders-api:1.0.0
docker pull localhost:5001/orders-api:1.0.0

# Or let the SDK push it, without the local Docker.
dotnet publish -t:PublishContainer -c Release -p:ContainerRegistry=localhost:5001
```

Amazon ECR (from the [AWS docs](https://docs.aws.amazon.com/AmazonECR/latest/userguide/docker-push-ecr-image.html); not run for this page):

```bash
aws ecr get-login-password --region eu-west-2 | docker login --username AWS --password-stdin 123456789012.dkr.ecr.eu-west-2.amazonaws.com
docker tag orders-api 123456789012.dkr.ecr.eu-west-2.amazonaws.com/orders-api:1.0.0
docker push 123456789012.dkr.ecr.eu-west-2.amazonaws.com/orders-api:1.0.0
```

The repository must exist first, and the sign-in lasts 12 hours.

## Scan with Trivy

```bash
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock -v trivy-cache:/root/.cache \
  aquasec/trivy:0.76.2 image --scanners vuln orders-api

# In CI: fail the build on anything high or critical.
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock -v trivy-cache:/root/.cache \
  aquasec/trivy:0.76.2 image --scanners vuln --severity HIGH,CRITICAL --exit-code 1 orders-api
```

On Rancher Desktop for macOS, `/var/run/docker.sock` needs Administrative Access: see [Docker](/platform/docker/#macos). To patch an image, pull the updated base and rebuild.
