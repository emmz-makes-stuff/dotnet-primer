# Tasks

## 1. Shared setup probe and package versions

- [x] 1.1 Probe the local setup (design D2, D3, D7). In a scratch `dotnet new web` app on 10.0.4xx:
  - Add the four OpenTelemetry packages at the D3 versions, with
    `AddOpenTelemetry().WithTracing().WithMetrics()`, `Logging.AddOpenTelemetry()` and
    `UseOtlpExporter()`.
  - Run the D2 `docker run` command (unsandboxed). Read the login token from `docker logs`, and find
    the current name of the anonymous-access environment variable.
  - Send requests, and confirm that the dashboard receives logs, a trace and metrics for the scratch
    service. Use its API or UI, and note what was seen.
  - Run `dnx dotnet-counters monitor` against the app, and record which built-in meters and
    instruments appear: `Microsoft.AspNetCore.Hosting`, `Microsoft.AspNetCore.Server.Kestrel`,
    `System.Net.Http`, `System.Runtime`.
  - Stop and remove the container.
  - Write a devlog note with the exact commands, the variable names and the instrument names
    observed.
- [x] 1.2 Add the D3 `PackageVersion` entries to `samples/Directory.Packages.props`. Verify that
  `make samples-build` and `make samples-format` pass (unsandboxed, for NuGet).

## 2. Exercise 04: logging

- [x] 2.1 Scaffold `samples/exercises/04-logging/` like 01–03 (design D5):
  - `Logging.Starter` and `Logging.Solution` web projects;
  - the shared `tests/`;
  - `Starter.Tests` and `Solution.Tests`.

  Add them to `Primer.slnx`. Add only the Solution and its tests to `Primer.Solutions.slnx`. Verify
  with `make samples-build`.
- [x] 2.2 Implement the Solution: `POST /orders` with the D5 payment fake, and `[LoggerMessage]`
  methods for "order placed" (Information, `OrderId`, `ItemCount`) and "payment declined" (Warning,
  `OrderId`, `Reason`), with no email anywhere.
  - Write the shared tests with `AddFakeLogging()`/`FakeLogCollector`. Cover all three spec
    scenarios, with assertion messages that name the missing behaviour.
  - Verify that `make samples-test` passes.
- [x] 2.3 Write the Starter. The endpoint works, but it logs badly (an interpolated Error message that
  includes the email), and it has `TODO`s.
  - Verify that `dotnet test --project samples/exercises/04-logging/Starter.Tests` fails with the
    guiding messages.
  - Complete a scratch copy, confirm that its tests go green, and discard it.

## 3. Logging page and cheatsheet

- [x] 3.1 Create `site/src/content/docs/cheatsheets/logging.md` (design D6), with a back link and
  Learn links. Add it to the sidebar's Cheatsheets group, the introduction's cheatsheet cards and
  `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [x] 3.2 Rewrite `site/src/content/docs/observability/logging.md` per D1 and D4. Cover every item in
  "Observability pages share one local setup", "Logging page gives logging guidance" and "Logging page
  covers the logging API", plus the D8 "In production" section with the AWS note.
  - Add the Pitfalls and the cheatsheet tip.
  - Add an "Exercise 04" section in the exercise-01 format, with the exact GitHub tree link.
- [x] 3.3 Compile every snippet in a scratch web project (warnings as errors). Confirm that the
  "from the exercise" snippets match the real files. Run the setup snippet against the dashboard, as
  in 1.1. Check the AWS note against the current ADOT docs (D8). Record all of it in the
  section-done note.
- [x] 3.4 Add `observability/logging` and `cheatsheets/logging` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Check every Learn and AWS link with the link script. Verify that `make gates` shows
  `GATES_EXIT:0`.

## 4. Exercise 05: tracing

- [ ] 4.1 Scaffold `samples/exercises/05-tracing/` like 2.1, with `Tracing.{Starter,Solution}[.Tests]`
  in both `.slnx` files the same way. Verify with `make samples-build`.
- [ ] 4.2 Implement the Solution (D5): `ActivitySource("Orders")`, the `PlaceOrder` and `ChargePayment`
  spans, the `order.id` and `order.item_count` attributes, and on gateway failure error status plus
  `AddException`. The response is 502.
  - Write the shared tests with an `ActivityListener`, filtered by the request's trace ID and in one
    xUnit collection. Cover all three spec scenarios.
  - Verify that `make samples-test` passes. Then run it twice more to check that it's stable.
- [ ] 4.3 Write the Starter, with the endpoint working and the source and spans as `TODO`s. Verify that
  the Starter tests fail with guiding messages, and that a completed scratch copy goes green.

## 5. Tracing page and cheatsheet

- [ ] 5.1 Create `site/src/content/docs/cheatsheets/tracing.md` (D6), and wire it in as 3.1. Verify with
  `make site-build`.
- [ ] 5.2 Rewrite `site/src/content/docs/observability/tracing.md` per D1 and D4. Cover "Tracing page
  gives tracing guidance", "Tracing page covers the tracing API" and the "In production" section with
  the AWS note.
  - Build on the Logging page's setup, without repeating it.
  - Add the Pitfalls, including "`StartActivity` returns null when the source isn't registered", and
    the cheatsheet tip.
  - Add the Exercise 05 section.
- [ ] 5.3 Compile and check the snippets as in 3.3.
  - Check in the dashboard that a custom span appears as a child of the request span.
  - Check that `StartActivity` returns `null` with no listener.
  - Record the results.
- [ ] 5.4 Add `observability/tracing` and `cheatsheets/tracing` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Run the link check. Verify that `make gates` shows `GATES_EXIT:0`.

## 6. Exercise 06: metrics

- [ ] 6.1 Scaffold `samples/exercises/06-metrics/` like 2.1, with `Metrics.{Starter,Solution}[.Tests]`
  in both `.slnx` files. Verify with `make samples-build`.
- [ ] 6.2 Implement the Solution (D5): `IMeterFactory`, with `orders.placed` (`{order}`; tags
  `payment.method`, `order.outcome`) and `orders.value` (`{GBP}`; tag `payment.method`).
  - Write the shared tests with `MetricCollector<T>`. They cover all three spec scenarios, including
    a check that only allowed tag keys appear.
  - Verify that `make samples-test` passes.
- [ ] 6.3 Write the Starter, with the endpoint working and the meter and instruments as `TODO`s. As a
  trap the tests catch, include a commented hint that tempts an `order.id` tag. Verify that the
  Starter fails with guiding messages, and that a completed scratch copy goes green.

## 7. Metrics page and cheatsheet

- [ ] 7.1 Create `site/src/content/docs/cheatsheets/metrics.md` (D6), and wire it in as 3.1. Verify
  with `make site-build`.
- [ ] 7.2 Rewrite `site/src/content/docs/observability/metrics.md` per D1 and D4. Cover "Metrics page
  gives metrics guidance", "Metrics page covers the metrics API" and the "In production" section with
  the AWS note.
  - Use the built-in meter and instrument names observed in 1.1.
  - Add the Pitfalls (cardinality, the wrong instrument, missing units) and the cheatsheet tip.
  - Add the Exercise 06 section.
- [ ] 7.3 Compile and check the snippets as in 3.3.
  - Run the page's `dnx dotnet-counters monitor` command against a scratch app, and see the custom
    and built-in metrics.
  - Check the custom metrics in the dashboard.
  - Record the results.
- [ ] 7.4 Add `observability/metrics` and `cheatsheets/metrics` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Run the link check. Verify that `make gates` shows `GATES_EXIT:0`.

## 8. Samples README and human check

- [ ] 8.1 Update `samples/README.md`:
  - the Layout tree and exercise table show 04–06, with their site pages;
  - the Docker note says 04–06 don't need Docker for their tests, and that the pages use Docker for
    the optional dashboard.

  Verify by reading it against `samples/exercises/`, and with `make gates`.
- [ ] 8.2 Human check (stop and ask). Emmz previews the site with
  `npm --prefix site run build && npm --prefix site run preview`, and checks:
  - the three pages and three cheatsheets, in light and dark;
  - the copy buttons;
  - searches for "cardinality" and "ActivitySource".

  She also runs the dashboard command from the Logging page with a scratch app, and sees a trace.
  Record her answer in a devlog note.

## Workflow follow-up

- Whole-change review, the change-done note, and the offer of `code-review`, per CLAUDE.md
  "Finishing a change".
- Merge to `main` and push, which deploys the site, then check the live pages.
- Offer `/opsx:archive`.
