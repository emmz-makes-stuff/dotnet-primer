# Probe 1.1: image facts, sizes and SDK publish on .NET 10

<!-- Section: 1 · Task: 1.1 · Base: ea386cb -->

**Platform:** this Mac, `linux/arm64` images. The engine is **OrbStack** (Docker 29.8.2), **not
Rancher Desktop**, which design D2 assumed. Image facts and command output don't depend on the
engine. Two Rancher-specific details can't be measured here: its socket path for Trivy's mount
(D8), and whether its CLI has `docker debug`. Both are covered below. SDK 10.0.404, both locally
(through the repo's `global.json`) and in `sdk:10.0`. Runtime 10.0.15.

**Gotcha:** a scratch folder outside the repo has no `global.json`, so `dotnet new` and `publish`
used the installed **11.0 RC** SDK, and built on `aspnet:11.0.0-rc.1`. Every scratch probe copies
the repo's `global.json` in first.

## Tags (MCR tag lists, 2026-11-11)

- `sdk`, `aspnet`, `runtime` and `runtime-deps` all have `10.0`, `10.0-noble`, `10.0-resolute`,
  `10.0-alpine`, `10.0-azurelinux3.0` and patch tags (`aspnet:10.0.15`, `sdk:10.0.404`).
- Chiseled: `aspnet`, `runtime` and `runtime-deps` have `10.0-noble-chiseled` and
  `10.0-noble-chiseled-extra`, and `-resolute-chiseled(-extra)` too. `aspnet` also has
  `-composite` variants. Azure Linux has `-distroless`.
- The AOT SDK is `sdk:10.0-noble-aot` (also `-aot`, `-alpine-aot`, `-resolute-aot`).
- **The default `10.0` tag is Ubuntu 24.04.5 LTS (noble).** MCR has no Debian tags for 10.0.
  `resolute` is Ubuntu 26.04. The page uses `noble`, matching the default, and names `resolute`
  as the newer choice.

## `dotnet publish -t:PublishContainer` (a `dotnet new web` app)

```
  Building image 'probe' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/aspnet:10.0'.
  Pushed image 'probe:latest' to local registry via 'docker'.
```

`docker inspect`: `User=1654`, port `8080/tcp`, `ASPNETCORE_HTTP_PORTS=8080`, `APP_UID=1654`,
entrypoint `["dotnet","/app/Probe.dll"]`, working directory `/app/`. The image is named after the
project (lower-cased), and tagged `latest`.

## The finding that changes D4's TODO 3

**`-p:ContainerFamily=noble-chiseled` built on `aspnet:10.0-noble-chiseled-extra`,** not plain
chiseled. The SDK adds `-extra` unless the app sets `InvariantGlobalization=true`, because plain
chiseled has no ICU. Plain chiseled sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` itself.

| Properties | Base the SDK picked |
| --- | --- |
| (none) | `aspnet:10.0` |
| `ContainerFamily=noble-chiseled` | `aspnet:10.0-noble-chiseled-extra` |
| `ContainerFamily=noble-chiseled`, `InvariantGlobalization=true` | `aspnet:10.0-noble-chiseled` |
| `ContainerBaseImage=…aspnet:10.0-noble-chiseled` | `aspnet:10.0-noble-chiseled` |

So TODO 3's fix is `ContainerFamily` **plus** `InvariantGlobalization`, and the page says why. Its
check (no `/bin/sh`) passes with either chiseled base, since `-extra` has no shell either.

## The default users

| Image | `Config.User` |
| --- | --- |
| `aspnet:10.0` | **empty, so root** |
| `aspnet:10.0-noble-chiseled`, `-extra` | `1654` |
| An SDK-published image on `aspnet:10.0` | `1654` (the SDK sets it) |
| A Dockerfile `FROM aspnet:10.0` with no `USER` | root |

So the page says: SDK publish is non-root by default, the full `aspnet` image isn't, and a Dockerfile
needs `USER $APP_UID` unless it's on chiseled.

## Sizes (`docker image ls` SIZE, `linux/arm64`, one `dotnet new web` app)

| Base | Image |
| --- | ---: |
| `aspnet:10.0` (Ubuntu 24.04) | 262 MB |
| `aspnet:10.0-noble-chiseled-extra` | 170 MB |
| `aspnet:10.0-noble-chiseled` | 132 MB |
| Native AOT on `runtime-deps:10.0-noble-chiseled` | 60.5 MB |

The bases alone: 262, 170, 131 and 12.6 MB. `sdk:10.0` is 976 MB. AOT was built in a Dockerfile
on `sdk:10.0-noble-aot`, with `PublishAot=true` and `InvariantGlobalization=true`, in 27 s. It runs
(`Now listening on: http://[::]:8080`).

## Dockerfile rebuild

Multi-stage: `sdk:10.0`, `COPY Probe.csproj`, `RUN dotnet restore`, `COPY . .`, `RUN dotnet publish
--no-restore`, then `aspnet:10.0-noble-chiseled` with `USER $APP_UID`. After appending a comment to
`Program.cs`:

```
#10 [build 4/6] RUN dotnet restore
#10 CACHED
#11 [build 5/6] COPY . .
#12 [build 6/6] RUN dotnet publish -c Release -o /app --no-restore
```

The image is 132 MB, with `User=1654`.

## Inspecting a container with no shell

- `docker exec <c> sh -c true` → `exec: "sh": executable file not found in $PATH`.
- **`docker debug` isn't in this CLI** (29.8.2, open source). It's a Docker Desktop feature, so the
  page names it as Docker Desktop's only. That Rancher Desktop's CLI lacks it too is inferred,
  because it ships the open-source CLI. It isn't measured.
- **What works:** a sidecar sharing the process namespace, `docker run --rm --pid=container:<c>
  busybox:1.37 cat /proc/1/status` → `Uid: 1654 …`. Also `docker cp` for files, `docker top`, and
  `docker exec <c> dotnet --list-sdks`, since `dotnet` is the one binary there. It prints nothing on
  a runtime image.

## Findings for D5 (exercise checks), to settle in 1.2

- **`docker cp <c>:/proc/1/status -` returns an empty file:** `/proc` files report size 0. So D5's
  non-root check through the file copy **doesn't work**.
- **`docker top <c>` (the Docker API's process list) reports the real user:** `1654`, or `root` for
  `--user root`. OrbStack's `ps` is BusyBox, so it rejects `-o uid`, but the default columns work.
  1.2 checks this from a test, and 2.5 on Linux CI, where dockerd's default is `ps -ef`, whose
  column is `UID`.
- `docker cp <c>:/bin/sh -` and `:/usr/share/dotnet/sdk -` exit 1 on chiseled, so the absence
  checks work through the copy API.

## Next

Task 1.2: the exercise test approach. Use `docker top` (the API's process list), not
`/proc/1/status`, for non-root, and confirm it from Testcontainers.
