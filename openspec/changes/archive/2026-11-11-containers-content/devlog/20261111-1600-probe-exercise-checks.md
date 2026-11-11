# Probe 1.2: exercise 12's checks work, through the Docker API

<!-- Section: 1 · Task: 1.2 · Base: ea386cb -->

A scratch app and tests, in the shape of D3 and D4. The app is a standalone `.csproj` (with its own
`TargetFramework`, `Nullable` and `ImplicitUsings`), a `.dockerignore` (`bin/`, `obj/`), and a
startup log line plus `GET /info` for `Warehouse:Name`. The tests use xUnit v3 4.0.2 and
Testcontainers 4.16.0, and get the app folder from `AssemblyMetadata`. They had no reference to
the app. OrbStack, `linux/arm64`.

## Results: every check tells right from wrong

| Image | `/info` with `Warehouse__Name` | `docker top` user | `/bin/sh` | `/usr/share/dotnet/sdk` |
| --- | --- | --- | --- | --- |
| SDK, right (`ContainerFamily` + `InvariantGlobalization`) | `"From env"` | `1654` | absent | absent |
| SDK, wrong (`aspnet:10.0`, `ContainerUser=root`) | `"From appsettings"` (no env given) | **root** | **present** | absent |
| Dockerfile, right (multi-stage, chiseled, `USER $APP_UID`) | `"From env"` | `1654` | absent | absent |
| Dockerfile, wrong (single `sdk:10.0` stage) | `"From appsettings"` (no env given) | **root** | **present** | **present** |
| SDK right, run with `ASPNETCORE_HTTP_PORTS=5000` | `HttpRequestException` on 8080 | `1654` | absent | absent |

The port case logged `Now listening on: http://[::]:5000`, so the test can name the port at once.

## The API calls (Testcontainers 4.16.0 uses `Docker.DotNet.Enhanced` 4.3.3)

- Client: `TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientBuilder(Guid).Build()`.
  It isn't `GetDockerClientConfiguration` (a guess that didn't compile).
- User: `client.Containers.ListProcessesAsync(id, new ContainerListProcessesParameters())`. The
  titles were `PID,USER,TIME,COMMAND` on OrbStack. Find the `USER` **or** `UID` column, because
  dockerd on Linux runs `ps -ef`. CI (2.5) checks the Linux side.
- Absence: `client.Containers.GetArchiveFromContainerAsync(id, new ContainerPathStatParameters {
  Path = … }, statOnly: true)`, which throws `DockerContainerNotFoundException` when the path is
  missing.
- **Ruled out:** `/proc/1/status` through the file copy, which returns an empty file because `/proc`
  reports size 0 (1.1). D5 is updated to say so. The `docker inspect` fallback that D5 kept in
  reserve isn't needed, so no stop.
- Dockerfile build: `ImageFromDockerfileBuilder().WithDockerfileDirectory(dir).WithDockerfile(f)`
  honours `.dockerignore`. A failing build throws `ImageBuildFailedException`, with `The command
  '/bin/sh -c exit 7' returned a non-zero code: 7` in its message. That's what the fixture puts in
  front of the engineer.
- SDK image: `dotnet publish -t:PublishContainer -c Release -p:ContainerRepository=… 
  -p:ContainerImageTag=…`, run as a child process from the test, works. A non-zero exit returns the
  build output.

## Logs

`GetLogsAsync()` returns lines prefixed with an RFC 3339 timestamp. The simple console formatter
writes the category on one line and the message on the next (`info: Program[0]` then
`      Warehouse: From env`). So the setting test looks for the line that contains `Warehouse:`,
not the line after it. The probe got that wrong, but `/info` confirmed the value.

## Timings (warm Docker cache; the base images were already pulled in 1.1)

- SDK publish: 3.8 s (right), 1.8 s (wrong).
- Dockerfile build: 1.0 s (right, cached), 2.9 s (wrong).
- Container start to `Now listening on`: about 1.2 s (2.4 s the first time).
- The whole probe test: 30 s, including 5 container runs, plus Testcontainers' Ryuk reaper.

A cold run also pulls `sdk:10.0` (976 MB) and the chiseled bases. 2.3 records the gate's time
before and after.

## Notes for §2

- Clean up: remove each built image in the fixture's `DisposeAsync`. The probe left
  `probe-sdk-*:probe` and `probe-df-*:probe` behind.
- `dotnet test` (MTP) hides test output. Run the test exe with `-showliveoutput` to see it.
- The file-based-program cache gotcha bit once (`build-start.cache` access denied, unsandboxed).
  `dotnet build-server shutdown` fixed it, as CLAUDE.md says.

## Next

Task 1.3: the page's extras (Compose, registry, Trivy, ECR syntax).
