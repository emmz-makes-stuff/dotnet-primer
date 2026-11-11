# Exercise 13: the Solution and its tests (2.1–2.3)

<!-- Section: 2 · Tasks: 2.1–2.3 (done), 2.4 next · Base: 8c34d23 -->

All of it is uncommitted until §2 lands.

## Decisions

- **Timings, all fixed in code:**
  - `Payments.Duration` is 3 s;
  - `DrainDelay.Duration` is 2 s, with a comment that production wants a few of the load
    balancer's intervals and 2 s keeps the tests quick;
  - `ShutdownTimeout` is 8 s, because 2 s + 3 s fit under it, and it fits under `docker stop`'s
    10 s and ECS's 30 s default. The comment says so;
  - the Starter will use 1 s (TODO 4).
- **`Store:Online` configuration** (default `true`): it starts the store offline, so that 2.2's
  "call both endpoints with the store up and down" works outside the tests, and the page can show
  `dotnet run --Store:Online=false`. The tests replace the store, so they don't use it.
- **`POST /orders` answers `503` (`"The order store is offline."`) while the store is offline.**
  Without this, an offline store still took orders. That isn't in the spec, but it keeps the
  readiness story honest. It's a problem response, not a health check.
- **The tests:**
  - `HealthOrdersApp` is `WebApplicationFactory<Program>`, with
    `UseKestrel(o => o.Listen(IPAddress.Loopback, 0))` in its constructor, environment `Testing`,
    and the `OrderStore` replaced. `BeginStop()` calls `StopApplication()` and returns a task that
    `ApplicationStopped` completes;
  - two classes, so that xunit runs them in parallel: `HealthTests` (an order can be fetched,
    TODO 1, TODO 2) and `ShutdownTests` (TODO 3, TODO 4). Every test pays the drain delay once at
    teardown, and the shutdown tests pay it again (`1906`).
- **TODO 3's test is the one timed check.** A single probe straight after `StopApplication()`
  could still get `503` from a server that has no delay, because Kestrel takes ~0.2–0.3 s to
  refuse. So the test polls ready (and live) every 100 ms for 1 s and expects every answer to be
  ready `503`. A refused connection fails with "no drain delay"; a `200` fails with "no
  shutting-down check". 1 s is half the Solution's delay. The behaviour under test is itself a
  length of time, so D5's "never on sleeps" doesn't apply here. Every other wait is on an event
  (the store's `OrderInProgress`, `ApplicationStopped`) with a 10 s patience.
- **Keeping each TODO's tests separate (for 2.4's leave-one-out check):**
  - TODO 3's test **skips** (`Assert.Skip`, "Do TODO 2 first") when `/health/ready` is `404`,
    instead of failing for TODO 2;
  - it checks that live stays `200` during the drain only if live ignored an offline store in a
    pre-check, that is, once TODO 1 is done. So leaving out TODO 1 doesn't fail TODO 3.

## Measured

- The Solution, run directly with SIGTERM 0.8 s into an order: ready `503`, live `200` for the
  2 s, the order is `201` at 3.08 s, and the exit code is 0. With `--Store:Online=false`: live
  `200`, ready `503`, and `POST /orders` gives `503`.
- `Solution.Tests`: 5 of 5 pass, **ten runs in a row**, at 10.32–10.46 s each.
- **`make samples-test`:** **34.51 s before** (a clean worktree of `8c34d23`, after a build) and
  **33.43 s after**, with 104 tests passing. Exercise 13's assembly took 10.7 s, but it runs in
  parallel with the others, so the wall time didn't grow.

## Next

2.4: write the Starter, with the four bugs and TODOs 1–4 (D4):
- live mapped with no options (it runs every check);
- no `/health/ready`, and the store's check untagged;
- no shutting-down check and no drain delay;
- `ShutdownTimeout` 1 s, "so deploys are quick".

`Orders.cs` stays identical to the Solution's. Then check that each test fails against it with its
guiding message, that a scratch copy with the Solution's code passes, and that leaving out one TODO
fails only that TODO's tests. `samples-build` (Primer.slnx) is red until the Starter compiles
against the shared tests.
