# Probe 1.2: health checks and shutdown, measured

<!-- Section: 1 · Task: 1.2 (done) · Base: 384453b -->

(The file name says 1906 so that it sorts after `1905`. The clock read 18:56 UTC when this was
written, so the earlier notes' stamps run a few minutes ahead.)

This note covers all of 1.2. It folds in `1900`'s direct-process results, and adds the container
and `WebApplicationFactory` runs.

**Platform and versions, 2026-11-11:** macOS 26.6.2 on Apple silicon. .NET SDK 10.0.404, with
`global.json` 10.0.400 and `latestFeature`; runtime 10.0.15. Docker 29.8.2 on OrbStack 2.2.5
(`aarch64`). `Microsoft.AspNetCore.Mvc.Testing` 10.0.14, xunit.v3 4.0.2, Node 24.13.1.

**The scratch app** (`hs/` in the session scratchpad, gone after a clear) is `1900`'s app with
`Probe:` settings: `Probe:OrderSeconds` (default 5), `Probe:DrainDelaySeconds` (default 0) and
`Probe:ShutdownTimeoutSeconds`. The drain delay is an `IHostedLifecycleService` that waits in
`StoppingAsync`. `hs/probe.mjs` posts an order, runs the stop command 0.75 s later, and polls live
and ready every 0.5 s. `hs/run.sh` wraps one container run.

## Direct process (from `1900`)

- With the store up: live `200 Healthy`, ready `200 Healthy`. With the store down: live
  `200 Healthy`, ready `503 Unhealthy`. These are the default writer's plain-text bodies.
- **The default `HostOptions.ShutdownTimeout` in .NET 10 is `00:00:30`**, logged at startup.
- SIGTERM during a 5 s order: `Application is shutting down...`, then the order completes with
  `201` and `Order 1 stored`, and the exit code is 0.
- `ShutdownTimeout` 1 s: the order's connection is reset at the timeout, and the order isn't
  stored. **TODO 4's bug is real.**
- An order that waits on `ApplicationStopping` fails at once with `500` (`TaskCanceledException`),
  which makes a good Pitfall.
- Without a drain delay, every new connection is refused about 0.3 s after SIGTERM, so a readiness
  check that fails on shutdown is never seen. A delay in a plain `StopAsync` runs after the server
  has stopped. A delay in `StoppingAsync` works (D3).

## In a container (D6)

The image is `dotnet publish -t:PublishContainer` with `ContainerFamily` `noble-chiseled` and
`InvariantGlobalization`. Its base image is `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`,
its entry point `["dotnet","/app/App.dll"]` and its user `1654`. It runs with
`-p 18081:8080`. The order starts at about 0.1 s, and `docker stop` runs at 0.86 s.

| Run | Order | Health during stop | Exit | Shutdown log |
| --- | --- | --- | --- | --- |
| 1: no `--init`, defaults | `201 {"id":1,"sku":"SKU-1"}` at 5.14 s | `ECONNRESET`, then `ECONNREFUSED` once the container is gone | `0` | `Application is shutting down...`, `Order 1 stored` |
| 2: `--init`, defaults | `201` at 5.15 s | the same as run 1 | `0` | the same as run 1 |
| 3: no `--init`, `ShutdownTimeout` 1 s | `UND_ERR_SOCKET` (socket closed) at 1.93 s | `ECONNRESET`, then `ECONNREFUSED` | `0` | `Application is shutting down...` only. **No `Order 1 stored`.** |
| 4: no `--init`, drain delay 3 s | `201` at 5.19 s | live `200 Healthy`, ready `503 Unhealthy`, from 1.16 s to 3.69 s; then `ECONNRESET` | `0` | `Application is shutting down...`, `Draining for 00:00:03 before the server stops`, `Health check shutting-down with status Unhealthy completed after 0.165ms with message 'The app is shutting down.'` (once per poll), `Order 1 stored` |
| 5: drain 30 s, `ShutdownTimeout` 60 s, `docker stop` default | `201` at 5.14 s | live `200`, ready `503` until the kill | **`137`** | killed partway through the drain |

- **SIGTERM reaches .NET as PID 1 without `--init`.** Runs 1 and 2 match, so `--init` isn't
  needed for shutdown. (Exercise 12 showed that it matters for a crash, not a stop.)
- **`docker stop`'s default timeout is 10 s.** In run 5, `docker stop` started at 18:54:10.543 and
  returned at 18:54:20.781, which is 10.24 s. Then the container exited with `137` (SIGKILL), with
  `OOMKilled=false`. `docker stop --help` says only `-t, --timeout int  Seconds to wait before
  killing the container`, with no default, so the page quotes the measured 10 s.
- **A short `ShutdownTimeout` loses the order, and the exit code stays 0.** Nothing in
  `docker inspect` shows that work was lost, which is a point for the page.
- Through Docker's port proxy, a new connection during shutdown gets `ECONNRESET`, not
  `ECONNREFUSED`, until the container is gone.
- The timeouts nest: drain delay plus the longest request has to fit in `ShutdownTimeout`, and
  that has to fit in the platform's stop timeout. Run 5 shows what happens when it doesn't.

## `WebApplicationFactory.UseKestrel` (D5)

The scratch `App.Tests` is xunit.v3 with `Microsoft.AspNetCore.Mvc.Testing`, `UseKestrel(...)`,
`StartServer()` and `CreateClient()`. It runs three cases: drain 3 s, drain 0, and drain 0 with
`ShutdownTimeout` 1 s.

- **`UseKestrel(0)` did not give a random port.** The app listened on `http://localhost:5000`, the
  default URL. After Kestrel stopped, requests got `403` from macOS ControlCenter, which holds port
  5000. **Use `factory.UseKestrel(o => o.Listen(IPAddress.Loopback, 0))`**: that listened on
  `http://127.0.0.1:60323` (60328 and 60332 on the next cases). `CreateClient()`'s `BaseAddress`
  is that real address, and a plain `new HttpClient { BaseAddress = ... }` reaches it too.
- **Stop with `IHostApplicationLifetime.StopApplication()`, not `IHost.StopAsync()`.** The
  factory runs the app's own `app.RunAsync()`, so `StopApplication()` follows SIGTERM's path:
  `ApplicationStopping` fires first, and then the host's `StopAsync` runs the `StoppingAsync`
  drain. Calling `IHost.StopAsync()` directly runs `StoppingAsync` **before** `ApplicationStopping`
  fires, so readiness stayed `200 Healthy` through the whole drain. That's the wrong test. Wait
  for `ApplicationStopped` to know the host has stopped.
- With `StopApplication()`, 0.2 s later:
  - **drain 3 s:** live `200 Healthy`, ready `503 Unhealthy`, and the order is `201` at 5.18 s,
    with 1 order stored;
  - **drain 0:** live and ready both `HttpRequestException`, `HttpRequestError.ConnectionError`
    ("Connection refused"), and the order is `201` at 5.10 s, with 1 stored. This is what TODO 3's
    test sees without the delay, so its failure message can name the missing delay;
  - **`ShutdownTimeout` 1 s:** the order fails at 1.09 s with `HttpRequestException` ("An error
    occurred while sending the request."), and 0 orders are stored. This is TODO 4's test, failing
    as it should.
- **Disposing the factory after the host has stopped runs `StoppingAsync` again**, so a drain
  delay costs a second wait at teardown (`Draining for 00:00:03` is logged after
  `host stopped`). The exercise should keep the delay short, make it configurable, or let the
  tests set it short through `UseSetting`. Decide in 2.3. The spec's "without changing how long an
  order takes" covers the order, not the delay.
- The three cases took 14.46 s in all. Each case is about 5 s, the order's length.

## For the design

- D5 needs `Listen(IPAddress.Loopback, 0)` and `StopApplication()`. Both are details inside D5's
  approach, not a change to it, so I'll fold them into D5 now, with no question for Emmz.
- D6's numbers are now measured: the default `ShutdownTimeout` is 30 s, `docker stop`'s default is
  10 s, `137` on a kill, and `--init` makes no difference to a stop.

## Next

1.3: probe Aspire (D7) with the Aspire CLI, unsandboxed, in a scratch folder: an AppHost with SQL
Server, the orders API (`WithReference`, `WaitFor`), and a second project that calls the API by
resource name. Record the CLI commands and output, the dashboard's address and sign-in, the
connection-string key, the SQL Server image, the versions, and what ServiceDefaults maps for health,
and in which environments.

Still uncommitted, landing in §1's one commit: 1.1's rows, checks, note and tick; the D3–D5 and
2.2 updates for the drain delay; this note and 1.2's tick.
