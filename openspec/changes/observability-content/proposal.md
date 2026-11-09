# Proposal

## Why

The engineers' current system has, in Emmz's words, "shockingly bad" observability. When something
goes wrong in production, they can't tell what happened, to which request, or how often. The three
Observability pages (Logging, Tracing, Metrics) are still placeholders, and they come straight after
the ASP.NET Core foundations in the learning path.

The engineers need more than the `ILogger`, `Activity` and `Meter` APIs. They need judgement:

- what to log, and at which level;
- what not to log;
- where a span belongs;
- which metric instrument to use, and which tags will bankrupt the metrics backend.

## What Changes

- **Write the full Logging page**, replacing the stub. It also opens the group:
  - **The three signals:** logs, traces and metrics, and which question each one answers.
  - **The one OpenTelemetry setup** the three pages share: `AddOpenTelemetry()`, OTLP export, and the
    **Aspire dashboard** container as the local viewer.
  - **Logging guidance:**
    - what to log and when;
    - choosing a level;
    - never logging secrets or personal data;
    - logging a failure once, where it's handled;
    - not logging in hot loops.
  - **The API:**
    - `ILogger<T>`, message templates and structured properties;
    - level filtering through configuration;
    - scopes;
    - the console and JSON formatters;
    - source-generated `[LoggerMessage]`;
    - log–trace correlation.
- **Write the full Tracing page:**
  - **Concepts:** traces, spans and W3C trace context, and how `ActivitySource`/`Activity` map to
    OpenTelemetry's tracer and span.
  - **Guidance:**
    - where to start a span, and where not to;
    - naming;
    - attributes from the semantic conventions;
    - recording errors with status and exception events;
    - events and links;
    - sampling;
    - baggage, with caution.
  - **The API:** automatic instrumentation for ASP.NET Core and `HttpClient`, registering your own
    source, and viewing a trace waterfall in the dashboard.
- **Write the full Metrics page:**
  - **When a metric is the right signal.** RED and USE as starting points.
  - **Choosing an instrument:** `Counter`, `UpDownCounter`, `Histogram`, `Gauge` and the observable
    ones.
  - **Naming and units,** following the semantic conventions.
  - **Tag cardinality,** and why a user or order ID must never be a tag.
  - **`IMeterFactory` with DI.**
  - **The built-in metrics** for ASP.NET Core, Kestrel, `HttpClient` and the runtime.
  - **`dotnet-counters`,** and export to the dashboard.
- **Production, vendor-neutral, with an AWS note.** The pages teach OTLP and the OpenTelemetry
  Collector as the seam between the app and a backend. A short section on each page says how the
  same telemetry reaches CloudWatch and X-Ray through the AWS Distro for OpenTelemetry (ADOT)
  collector. Deployment detail stays with the Orchestration & AWS page.
- **Three cheatsheets:** Logging, Tracing and Metrics. Each is linked both ways with its page,
  listed in the sidebar and on the introduction, and added to the site tests' maps.
- **Three exercises:** `04-logging`, `05-tracing` and `06-metrics`. Each has a starter, a solution
  and shared tests, like 01–03. Their tests use `FakeLogger`, an `ActivityListener` and
  `MetricCollector<T>`, so they need no Docker. Each is described on its page.
- **Gates:** the six new slugs go into `FULL_CONTENT_SLUGS`, and the three pairs into `CHEATSHEETS`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" now lists the three Observability pages and their
    cheatsheets.
  - It adds "Logging page coverage", "Tracing page coverage" and "Metrics page coverage". Each one
    requires the guidance as well as the API.
  - It adds "Observability pages share one local setup", covering OpenTelemetry, OTLP, the Aspire
    dashboard, vendor-neutral export, and the AWS note.
- **`site-structure`:** "Cheatsheets per area" grows from four cheatsheets to seven.
- **`sample-exercises`:**
  - "Site describes each exercise" gains scenarios for exercises 04–06.
  - It adds three requirements, one each for the Logging, Tracing and Metrics exercises.

## Impact

- **Site:**
  - `site/src/content/docs/observability/{logging,tracing,metrics}.md`;
  - three new `site/src/content/docs/cheatsheets/{logging,tracing,metrics}.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`).
- **Samples:**
  - `samples/exercises/04-logging`, `05-tracing` and `06-metrics`;
  - both `.slnx` files;
  - new `PackageVersion` entries in `samples/Directory.Packages.props`: OpenTelemetry packages and
    `Microsoft.Extensions.Diagnostics.Testing`;
  - `samples/README.md` (layout and exercise table).
- **Tooling:** no new machine dependencies.
  - Docker is already listed, and the Aspire dashboard runs on it.
  - `dotnet-counters` runs through `dnx`, without a global install.
- **Out of scope:**
  - the full Aspire app model (AppHost, service defaults);
  - production deployment of a Collector;
  - CloudWatch/X-Ray setup beyond the short note;
  - EF Core and gRPC instrumentation, which belong to those pages' changes;
  - logging/tracing in tests beyond the exercises' own use.
