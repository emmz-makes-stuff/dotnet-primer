# Emmz's answer: keep TODO 3, with a drain delay

<!-- Section: 1 · Task: 1.2 (unticked, partly done) · Base: 384453b -->

Emmz chose **A** for `1900-stopped-readiness-on-shutdown`'s question, on 2026-11-11: keep TODO 3,
and make the fix the stopping check **plus** a few seconds' drain delay in
`IHostedLifecycleService.StoppingAsync`.

Done in this session, all uncommitted:
- **design.md:** D3 has a drain-delay bullet (why it's needed, why `StoppingAsync` and not a plain
  `StopAsync`, both measured, and how the delay and the longest request fit inside
  `ShutdownTimeout` and the stop timeout). D4's TODO 3 includes the delay. D5's TODO 3 test expects
  ready `503` and live `200` during the delay, and says that a refused connection means there's no
  delay.
- **tasks.md:** 2.2 mentions the drain delay.
- The specs are unchanged, because their wording ("reports unhealthy once the app starts shutting
  down") holds. `openspec validate --strict` passes.

Still uncommitted from before: 1.1's rows, checks, note and tick (see `1900`).

## Next

Finish **1.2**, from `1900`'s "Not yet probed" list:
- rebuild the scratch app from `1900`'s description, with the drain delay as a `Probe:` setting;
- `docker stop`'s default timeout;
- `docker stop` with an order in flight, on the SDK-published image, with and without `--init`,
  and with the short `ShutdownTimeout`, recording the order's response and the shutdown log lines;
- `WebApplicationFactory.UseKestrel` on .NET 10: a random port, and whether stopping it drains a
  request and shows the drain delay's `503`.

Then write 1.2's probe note (fold in `1900`'s results), tick 1.2, and go on to 1.3 (Aspire) and 1.4
(AWS).

Gotcha: run HTTP from a Node script file (`node probe.mjs`), not inline in Bash. The scratch app's
analyzers flagged CA1873 on a logging argument that called `GetRequiredService`, so compute it into
a local first.
