# Stopped in 1.2: readiness can't fail during shutdown without a drain delay

<!-- Section: 1 · Task: 1.2 (unticked, partly done) · Base: 384453b -->

**Uncommitted:**
- 1.1's work: the `aspire` and `aws` rows in `docs/dependencies.md`, their `make deps` checks in the
  `Makefile`, and `devlog/20261111-1842-section-1-start-tool-rows.md`;
- 1.1's tick in `tasks.md`;
- this note.

Nothing in §1 is committed. All of it lands in §1's one commit.

## Probe so far (1.2)

The scratch app is in the session scratchpad, at `hs/App` (gone after a clear), with
`hs/probe.mjs`. It's a web app with the samples' `Directory.Build.props` (warnings as errors,
analyzers), `Directory.Packages.props` and `global.json` copied in, so it builds on 10.0.404. It
has:
- an in-memory store with an `Online` switch and an in-progress signal;
- `POST /orders`, which waits 5 s;
- `/health/live` (`Predicate = _ => false`) and `/health/ready` (tag `ready`: a store check, and a
  check that's unhealthy once `ApplicationStopping` has fired).

The app ran directly (`dotnet App.dll`, not `dotnet run`), so SIGTERM reached it. `probe.mjs` makes
the HTTP calls: context-mode's hook didn't block `node <file>`, only inline HTTP in the command
text. macOS, 2026-11-11:

- **The health responses:** with the store up, live `200 Healthy` and ready `200 Healthy`. With
  the store down, live `200 Healthy` and ready `503 Unhealthy`. These are the default writer's
  bodies.
- **The default `HostOptions.ShutdownTimeout` in .NET 10 is `00:00:30`** (logged at startup).
- **Default timeout, SIGTERM at 0.76 s during a 5 s order:** `Application is shutting down...`,
  then the order completes (`201 {"id":1,"sku":"SKU-1"}` at 5.28 s), `Order 1 stored`, exit 0.
  **Draining works.**
- **`ShutdownTimeout` = 1 s:** the client gets `ECONNRESET` at 1.76 s, and the order is never
  stored. The process exits 0. It doesn't matter whether the order waits on the request's token or
  no token: Kestrel resets the connection at the timeout either way. **TODO 4's bug is confirmed**
  as D4 describes it.
- **The order waiting on `ApplicationStopping`:** it fails at once, with `500` at 0.77 s and
  `TaskCanceledException` in the log. That's a good Pitfall.
- **The finding that blocks TODO 3:** 0.3 s after SIGTERM, *every* new request gets
  `ECONNREFUSED`, over a fresh connection or a kept-alive one, for live and ready alike. Kestrel
  stops accepting as soon as the host stops. So a readiness check that fails on
  `ApplicationStopping` is **never seen by anyone**. The in-flight order still drains.
- **A hosted service whose `StopAsync` waits 3 s doesn't help.** Kestrel still refuses at once,
  and the delay runs after the drain (exit at 8.28 s).
- **A delay in `IHostedLifecycleService.StoppingAsync` works.** It runs before any service's
  `StopAsync`, so before Kestrel stops. With 3 s: 0.3 s after SIGTERM, ready gives `503 Unhealthy`
  (on a fresh connection and a kept-alive one) and live gives `200 Healthy`. The log shows `Health
  check shutting-down with status Unhealthy … 'The app is shutting down.'`. The order still
  completes (`201` at 5.27 s), and the process exits 0 at 5.28 s.

**Not yet probed in 1.2:**
- `docker stop`'s default timeout, and `docker stop` with an order in flight (with and without
  `--init`, and with the short timeout);
- .NET 10's `WebApplicationFactory.UseKestrel`, and whether stopping it drains a request.

## The question for Emmz

TODO 3 ("readiness stays healthy during shutdown") can only be observed if the app keeps serving
for a few seconds after SIGTERM: a "drain delay" in `StoppingAsync`. That's the realistic pattern,
because a load balancer needs a few probe intervals to notice a 503 and stop sending traffic. It
also means the exercise's fix is two parts: the stopping check, plus the delay. And the delay plus
the drain has to fit inside `ShutdownTimeout` and the platform's stop timeout.

Context for the choice: on ECS behind an ALB, ECS deregisters the task from the target group (and
waits out the deregistration delay) **before** it sends SIGTERM, if the AWS docs say what I expect.
The probe will check that from the docs. If so, the delay matters most on EKS and other load
balancers. The page could say so either way.

- **A. Keep TODO 3, with a drain delay (recommended):** the fix adds the stopping check and a short
  `StoppingAsync` delay. The tests see `503` from ready and `200` from live during it. D3, D4 and
  D5 get the delay, and the spec's wording ("reports unhealthy once the app starts shutting down")
  still holds.
- **B. Drop TODO 3:** three TODOs. The page explains that Kestrel stops accepting at once, and that
  the platform drains the load balancer first. The spec's "App starts shutting down" scenario and
  the page requirement's "how to fail readiness once shutdown starts" are reworded.

- **On A:** update design D3–D5 and the tasks (an openspec-update of the change), re-validate, then
  finish 1.2's remaining probes: `docker stop` and `UseKestrel`.
- **On B:** update the specs (sample-exercises, topic-page-content), D3–D5 and the tasks, then
  finish the same probes.

## Next

Waiting on Emmz's answer to A or B above. Then finish 1.2: the docker stop probes, with
`--init`, and `WebApplicationFactory.UseKestrel` (rebuild the scratch app from this note's
description: it's gone after a clear). Then 1.3 and 1.4.
