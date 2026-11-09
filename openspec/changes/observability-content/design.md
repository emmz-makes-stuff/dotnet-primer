# Design

## Context

- **The three stubs** (`observability/{logging,tracing,metrics}.md`) each have a "What this covers"
  paragraph, Learn links and the "Coming soon" note. They're already in `EXPECTED_SLUGS` and in the
  sidebar's Learning path, between Middleware and EF Core. Neither the pages nor any cheatsheet is in
  `FULL_CONTENT_SLUGS` or `CHEATSHEETS`.
- **The conventions are set** in `site/README.md`: the topic skeleton (outcome, cheatsheet tip,
  concepts, Pitfalls, exercise), the cheatsheet shape, and the code-sample rules. So is the exercise
  layout in `samples/README.md` (Starter, Solution, shared `tests/`, `Starter.Tests`,
  `Solution.Tests`, both `.slnx` files, CPM in `samples/Directory.Packages.props`).
- **Decisions Emmz has made** (2026-11-09):
  - the Aspire dashboard container is the local viewer;
  - export is vendor-neutral, with a short AWS note;
  - one exercise per page;
  - one cheatsheet per page.
- **Current versions** (from NuGet and MCR on 2026-11-09):
  - `OpenTelemetry.Extensions.Hosting` and `.Exporter.OpenTelemetryProtocol` 1.20.1 (1.21 is still
    an RC);
  - `.Instrumentation.AspNetCore` and `.Instrumentation.Http` 1.20.2;
  - `Microsoft.Extensions.Diagnostics.Testing` 10.11.0;
  - the `mcr.microsoft.com/dotnet/aspire-dashboard` image at 13.6.4.
- **.NET 10 runtime metrics** are built in (meter `System.Runtime`), so
  `OpenTelemetry.Instrumentation.Runtime` isn't needed.
- **Docker** is already in `docs/dependencies.md`. `dnx` (SDK 10) runs tools such as
  `dotnet-counters` without installing them. The auto-mode classifier blocks `dotnet tool install`,
  but `dnx` ran fine in getting-started-content.

## Goals / Non-Goals

**Goals:**
- An engineer finishes the three pages knowing what to emit, where, and at what cost, as well as how.
  Every guidance point comes with a concrete do/don't example.
- Every snippet compiles on .NET 10.0.4xx with warnings as errors.
- The local setup is verified by running it: the dashboard shows logs, a trace and metrics from a
  scratch app.
- The three exercises are testable without Docker, and their tests fail with messages that guide.

**Non-Goals:**
- The Aspire AppHost or ServiceDefaults.
- A production Collector configuration, or a deployment.
- CloudWatch or X-Ray setup steps. The AWS note links out.
- Third-party logging libraries, such as Serilog or NLog. One line notes they exist and plug into
  `ILogger`.
- EF Core and gRPC instrumentation, which belong to those pages' changes.

## Decisions

### D1. The Logging page opens the group and owns the shared setup

Logging is first in the sidebar, so it opens with:
1. "Three signals, three questions": logs say *what happened to this request*, traces say *where the
   time went across services*, and metrics say *how often and how much, over time*. Each comes with
   one example from an orders service.
2. "One setup for all three", shown once on this page:
   - `builder.Services.AddOpenTelemetry().ConfigureResource(…).WithTracing(…).WithMetrics(…)`,
     plus `builder.Logging.AddOpenTelemetry(…)`;
   - `.UseOtlpExporter()`;
   - the service name, set with `OTEL_SERVICE_NAME`;
   - the dashboard `docker run` command.

Tracing and Metrics start with "Building on the setup from [Logging](…)" and show only their own
`.WithTracing`/`.WithMetrics` additions.

- **Rejected: a separate "Observability overview" page.** It would add a sidebar entry and slug
  outside the agreed scope.
- **Rejected: repeating the setup on each page.** Three copies would drift.

Each page follows the topic skeleton: outcome, cheatsheet tip, concepts, Pitfalls, Exercise. The
**guidance comes before the API** in each page, because judgement is what's missing.

### D2. Local viewer: the standalone Aspire dashboard container

```sh
docker run --rm -d --name aspire-dashboard \
  -p 18888:18888 -p 4317:18889 -p 4318:18890 \
  mcr.microsoft.com/dotnet/aspire-dashboard:13.6
```

- The UI is on `http://localhost:18888`. OTLP/gRPC is on `4317`, and OTLP/HTTP on `4318`. Those are
  the OTLP defaults, so the app needs no endpoint configuration.
- **The login token:** the page shows how to read it from `docker logs aspire-dashboard`. It also
  shows the documented "unsecured" switch, as an option for a local machine only.
- **Verify the env var name** in task 1.1. It changed between Aspire versions.
- **Pin the minor tag** (`13.6`), not `latest`, so the screenshots of behaviour and the flags stay
  true.
- **Rejected:**
  - **Grafana otel-lgtm:** heavier, with four UIs to learn.
  - **Console exporter only:** no waterfall view, which is the thing that makes tracing click.

### D3. OpenTelemetry packages and wiring

Add `PackageVersion` entries to `samples/Directory.Packages.props`:
- `OpenTelemetry.Extensions.Hosting` and `OpenTelemetry.Exporter.OpenTelemetryProtocol` at 1.20.1;
- `OpenTelemetry.Instrumentation.AspNetCore` and `OpenTelemetry.Instrumentation.Http` at 1.20.2;
- `Microsoft.Extensions.Diagnostics.Testing` at 10.11.0.

The pages pin these versions in prose ("at the time of writing"), and link to NuGet.

The wiring taught is `UseOtlpExporter()`, the cross-signal exporter, configured by the standard
`OTEL_EXPORTER_OTLP_*` environment variables. That's the same seam a Collector or the ADOT
collector uses (D8).

The exercises' **Solution** projects reference only what their tests need:
- the OTel packages only where the exercise registers OpenTelemetry;
- the logging and metrics exercises use only `Microsoft.Extensions.*` and `System.Diagnostics.*`.

That keeps each exercise about its signal.

### D4. Guidance content (the "judgement" sections)

Each guidance point gets a criterion and a do/don't pair:

- **Levels** get a table:
  - **Trace/Debug:** developer detail, off in production.
  - **Information:** a business-meaningful event, such as an order placed or a job finished, roughly
    one per unit of work.
  - **Warning:** something unexpected was handled and the system carried on (a declined payment, a
    retry).
  - **Error:** the unit of work failed and someone may need to act.
  - **Critical:** the process or a dependency is unusable.
- **What not to log:** secrets, tokens, passwords, connection strings, full request bodies, and
  personal data such as emails, names and addresses. Log identifiers instead. One sentence mentions
  `Microsoft.Extensions.Compliance.Redaction`, with a Learn link, as the option when data must be
  logged.
- **Log once, where handled:** catch-log-rethrow duplicates entries. Let the exception reach the
  handler that decides what happens, and log there. The Middleware page's exception handler already
  logs unhandled ones.
- **Hot paths:** the `[LoggerMessage]` source generator, `IsEnabled`, and no allocation-heavy
  arguments.
- **Spans** belong at boundaries and meaningful steps: incoming requests, outgoing calls, queue
  messages, and a business step worth timing. Not every method.
  - **Names** are low-cardinality (`PlaceOrder`, not `PlaceOrder 1234`). IDs go in attributes, where
    high cardinality is fine.
  - **Errors** use `SetStatus(ActivityStatusCode.Error)` plus `AddException(ex)` (.NET 9+).
- **Sampling:** parent-based ratio sampling, with the warning that the head decision drops whole
  traces.
- **Baggage** crosses service boundaries, and into third parties. Never put secrets or personal data
  in it.
- **Metrics:**
  - **Choosing an instrument:** a counter for things that only go up (orders); an up-down counter
    for in-flight work; a histogram for distributions such as durations and sizes; a gauge or an
    observable gauge for sampled values such as queue length.
  - **RED** (rate, errors, duration) for services; **USE** (utilisation, saturation, errors) for
    resources.
  - **Cardinality:** each distinct tag combination is a time series. Use bounded enums (method,
    outcome, status class), never IDs, emails or free text.
  - **Names** are dotted and lower-case, with the unit declared on the instrument (`s`, `By`,
    `{order}`), following the semantic conventions.

### D5. Exercises 04–06: one small orders app, three signals

All three share a domain:
- `POST /orders` with `{ customerEmail, paymentMethod, items: [...], total }`;
- a provided `IPaymentGateway` fake:
  - it declines `paymentMethod: "voucher"` for exercise 04 and 06's "declined" case;
  - it throws `PaymentGatewayException` when `customerEmail` ends in `@fail.test`, for 05's failure
    case;
- responses: 201 when accepted, 402 when declined, 502 when the gateway fails.

Each exercise is self-contained, as 01–03 are, with no shared project. The Starter has `TODO`s, and
the endpoint already works functionally. Only the instrumentation is missing, so the tests are about
telemetry alone.

- **04-logging.** Tests register `AddFakeLogging()` in a `WebApplicationFactory` and read
  `FakeLogCollector.GetSnapshot()`. They assert:
  - one Information "order placed" entry with `OrderId` and `ItemCount` in its structured state;
  - one Warning with `OrderId` and `Reason` on decline, and no Error;
  - no message or state value contains the email.

  The Starter logs badly on purpose: an interpolated string at Error that includes the email. So the
  exercise is to fix it, which matches the readers' real situation.
- **05-tracing.**
  - The Solution has a static `ActivitySource("Orders")` and spans `PlaceOrder` and `ChargePayment`,
    with attributes `order.id` and `order.item_count`, and on failure error status plus
    `AddException`.
  - Tests attach an `ActivityListener` for `Orders` and `Microsoft.AspNetCore`, and filter the
    recorded activities by the trace ID of the request they sent.
  - They assert the parent/child chain, the attributes, and the error status with an `exception`
    event carrying `exception.type` and `exception.message`.
- **06-metrics.**
  - The Solution creates a `Meter("Orders")` through `IMeterFactory`, with:
    - `Counter<long> orders.placed` (unit `{order}`), tagged `payment.method` and `order.outcome`;
    - `Histogram<double> orders.value` (unit `{GBP}`), tagged `payment.method`.
  - Tests use `MetricCollector<T>(meterFactory, "Orders", name)` and assert the counts per tag
    combination and the recorded value.
  - They check that the unit is set, and that every recorded tag key comes from the allowed set
    (`payment.method`, `order.outcome`).

Assertion messages name the missing behaviour, for example "expected one Information log with an
OrderId property for the placed order".

### D6. Cheatsheets

Each follows the cheatsheet shape (back link, `##` groups of copyable blocks, minimal prose):
- **Logging:**
  - `ILogger` calls with templates;
  - the `appsettings.json` level filters;
  - scopes;
  - `[LoggerMessage]`;
  - the JSON console formatter;
  - OTel log export;
  - a level-choice table, and a "never log" list.
- **Tracing:**
  - the `ActivitySource`/`StartActivity` pattern;
  - attributes, status and exception;
  - events and links;
  - registering a source;
  - the instrumentation packages;
  - the sampler;
  - the `traceparent` header format;
  - the dashboard command.
- **Metrics:**
  - the instruments, with a one-line "use for" each;
  - `IMeterFactory` registration;
  - recording with tags;
  - the built-in meter names, and the `dnx dotnet-counters monitor` commands;
  - the semantic-convention names and units for common cases.

### D7. Code accuracy and verification

As in the previous changes:
- every snippet is compiled in a scratch `dotnet new web` project, net10.0, with warnings as errors;
- Learn links are checked with the link script;
- snippets shown as "from the exercise" are copies of the real files.

Also:
- **The D2 dashboard command** is run (Docker, unsandboxed). A scratch app's logs, trace and metrics
  are seen in the dashboard API or UI, and that's recorded in a devlog note. The human check confirms
  the UI.
- **`dnx dotnet-counters monitor`** is run against the scratch app, and its output is recorded.
- **Built-in metric names and meters** are checked against the running app, not only against
  documentation. That covers `http.server.request.duration`, `kestrel.active_connections`,
  `http.client.request.duration` and `System.Runtime`'s `dotnet.*`.

### D8. Production export and the AWS note

Each page ends its concepts with a short "In production" section:
- export OTLP to an OpenTelemetry Collector, which batches, filters and fans out;
- the app only knows `OTEL_EXPORTER_OTLP_ENDPOINT`.

The AWS note: run the **AWS Distro for OpenTelemetry (ADOT)** collector as an ECS sidecar or EKS
DaemonSet. It sends traces to X-Ray, metrics to CloudWatch (EMF), and logs to CloudWatch Logs, with
no change to the app. It links to the ADOT docs and to the site's Orchestration & AWS page.

The AWS claims are checked against the current ADOT documentation when the pages are written. If
ADOT's guidance has moved (for example, toward CloudWatch's native OTLP endpoints), the note says
what the docs say then.

### D9. Gate additions

- **`EXPECTED_SLUGS`** gains `cheatsheets/logging`, `cheatsheets/tracing` and `cheatsheets/metrics`.
- **`FULL_CONTENT_SLUGS`** gains the three pages and the three cheatsheets.
- **`CHEATSHEETS`** gains the three pairs.
- **The sidebar's Cheatsheets group and the introduction's cheatsheet cards** gain the three.
- **The existing "every exercise folder is linked" test** covers 04–06 with no change.
- **No new test types.**

### D10. Section order

1. The shared setup probe and package versions (D2, D3, D7).
2. Exercise 04.
3. The Logging page and cheatsheet.
4. Exercise 05.
5. The Tracing page and cheatsheet.
6. Exercise 06.
7. The Metrics page and cheatsheet.
8. The samples README, then the human check.

Writing each exercise before its page means the page's "from the exercise" snippets are real, as in
aspnet-core-foundations-content.

## Risks / Trade-offs

- **`ActivityListener` is process-wide, so parallel tests can see each other's spans.** Mitigation:
  filter by the request's trace ID (D5), and put the tracing tests in one xUnit collection.
- **OpenTelemetry and semantic-convention names keep moving.**
  - Pin package versions.
  - Use only stable semantic conventions: `http.*`, and `exception.*`.
  - Say "at the time of writing" where it matters.
  - Link the spec pages.
- **The dashboard image and its flags change between Aspire releases.** Mitigation: pin `13.6`, and
  verify the token and anonymous-access variables in task 1.1.
- **The guidance is opinion.** Mitigation: each point gives its reason, and links the OpenTelemetry
  or Learn guidance it follows. Where teams legitimately differ (for example, Debug logging in
  production), the page says so.
- **The AWS note could be stale or wrong.** Mitigation: D8's check against current ADOT docs, and a
  short note that defers detail to the AWS page.
- **`{GBP}` as a unit is a judgement call.** UCUM has no currency units, and annotation units in
  braces are the OTel-recommended form. The page says so.
