# Design

## Context

- **The stub** `deploy/orchestration-and-aws.md` has a "What this covers" paragraph, two Learn links
  ("Host and deploy ASP.NET Core", "Health checks in ASP.NET Core"), a note that Aspire's docs moved
  to aspire.dev with the AWS starting points, and the "Coming soon" note. It's in `EXPECTED_SLUGS`
  and in the sidebar's Learning path, after Containers. Neither it nor a cheatsheet is in
  `FULL_CONTENT_SLUGS` or `CHEATSHEETS`.
- **Pages already point here:**
  - the Containers page (app health checks, under "Compose locally");
  - the three Observability pages (deployment, through the "production export" requirement).

  Those links stay valid. The Containers page's may be retargeted to an anchor (D1).
- **The conventions are set,** as for containers-content:
  - `site/README.md` gives the topic skeleton and the cheatsheet shape;
  - `samples/README.md` gives the exercise layout: `Starter`, `Solution`, and a shared `tests/`
    compiled into `Starter.Tests` and `Solution.Tests`, in both `.slnx` files;
  - xUnit v3 (ADR-0003), central package management, and warnings as errors from
    `samples/Directory.Build.props`.

  Exercise 11's tests are the model for 13's: they run the app in process with
  `WebApplicationFactory`, and use a fake the tests control (its warehouse) in place of the
  dependency.
- **Tools on this Mac** (2026-11-11), none of them in `docs/dependencies.md` yet:
  - Aspire CLI 13.6.0, with the `aspire-apphost` template installed;
  - AWS CLI 2.36.34;
  - no `Amazon.Lambda.Tools` (`dotnet lambda`).
- **Engine:** OrbStack on Apple silicon, as in containers-content, so SQL Server runs under
  emulation. Containers-content's PID 1 finding: a .NET app as PID 1 that crashes keeps running, and
  `--init` fixes it. That was a crash. A clean stop on SIGTERM wasn't measured.
- **Decisions Emmz made** (2026-11-11): see proposal.md. In short:
  - health and shutdown, Aspire, and AWS, but **no Kubernetes**;
  - AWS steps from the docs, syntax-checked and marked as not run;
  - exercise 13 on health checks and shutdown;
  - the Aspire CLI;
  - an AWS CLI row for syntax checks;
  - the Aspire example in a scratch folder only.
- **The site gate goes red during the exercise section.** "Undescribed exercise is caught" fails as
  soon as `13-health-and-shutdown` exists, until the page links to it, as with 11 and 12 (D10).

## Goals / Non-Goals

**Goals:**
- An engineer finishes the page able to give a service liveness and readiness endpoints that mean
  what an orchestrator expects, make it stop without dropping work, run it locally with its
  database through Aspire, and put it on ECS, EKS or Lambda with its configuration from AWS.
- Every output on the page that isn't from AWS is measured, with the platform and the date.
  Everything from AWS is linked to its source, syntax-checked where a tool can check it, and marked
  as not run.
- Exercise 13's tests are deterministic on a shared CI runner, with no timing races and no Docker.

**Non-Goals:**
- Kubernetes manifests, probes in YAML, Helm and a local cluster. EKS is described from the AWS
  side only.
- Infrastructure as code. Terraform and CloudFormation are named, with links, and nothing more.
- Aspire's deployment story (`aspire publish`, `aspire deploy`, Aspire's AWS integrations). It's
  named only.
- Changing the gRPC page's health-check section. It reports the same ASP.NET Core checks, and the
  page links to it.

## Decisions

### D1. Page shape and order

The topic skeleton, in this order:
1. The outcome, and the cheatsheet tip.
2. **Health checks:**
   - `AddHealthChecks` and `MapHealthChecks`;
   - liveness against readiness, and why liveness ignores the database;
   - a dependency check;
   - tags, and a predicate for each endpoint;
   - the response a probe sees (status code and body).
3. **Graceful shutdown:**
   - the signal path (`docker stop`, or ECS stopping a task, sends SIGTERM; SIGKILL follows after
     the stop timeout);
   - Kestrel stops taking requests and drains;
   - `HostOptions.ShutdownTimeout`, and how it relates to the container's stop timeout;
   - failing readiness on `ApplicationStopping`;
   - the measured `docker stop` run (D6), and a link back to the Containers page's PID 1 Pitfall.
4. **.NET Aspire locally** (D7), with a short "Aspire or Compose?" comparison.
5. **Running on AWS** (D8):
   - a "which one?" table for ECS on Fargate, EKS and Lambda;
   - then **ECS** (the task definition, the task role, the load balancer's health check);
   - **EKS** (the AWS side);
   - **Lambda** (`AddAWSLambdaHosting`).
6. **Configuration from AWS:** Parameter Store and Secrets Manager through the configuration
   provider, and credentials from the role.
7. **Coming from .NET 3.5:** the table.
8. **Pitfalls,** then **Exercise 13**.

The health and shutdown examples are exercise 13's Solution ("from the exercise" blocks), as on the
Containers page. The Containers page's pointer to this page for app health checks is retargeted to
`#health-checks`, once that anchor exists.

### D2. Measured, not remembered

The probe (§1) measures everything the page states that doesn't need AWS, on this Mac
(`linux/arm64`, OrbStack), and records the platform and the date:
- **the health endpoints' real responses:** status code and body, for live and ready, with the
  store up, with it down, and during shutdown;
- **the default `HostOptions.ShutdownTimeout` in .NET 10, and `docker stop`'s default timeout.**
  The page mustn't name either from memory;
- **`docker stop` with a request in flight** (D6);
- **the Aspire run** (D7): the CLI commands and their output, the dashboard's address and sign-in,
  and the API reaching SQL Server;
- **current versions** of the AWS packages (D8), and that each snippet compiles against them.

Each AWS fact is from a fetched page, with its URL recorded in the probe note.

### D3. Health checks: tags and predicates

Each check is registered with tags, and each endpoint maps the checks its probe needs:
- `/health/live` runs no dependency checks (`Predicate = _ => false`): if the app can answer, it's
  alive;
- `/health/ready` runs the checks tagged `ready`: the store's check, and a check that turns unhealthy
  once `IHostApplicationLifetime.ApplicationStopping` fires.
- **A drain delay makes that last check observable** (probe 1.2, Emmz chose it on 2026-11-11).
  Kestrel refuses every new connection as soon as the host stops, so without a delay nobody ever
  sees readiness fail. An `IHostedLifecycleService` waits a few seconds in `StoppingAsync`, which
  runs before any hosted service's `StopAsync`, and so before Kestrel's. While it waits, ready
  answers `503`, live answers `200`, new requests are still served, and the load balancer has time
  to notice and stop sending. Then Kestrel stops and drains. A delay in a plain hosted service's
  `StopAsync` doesn't work: it runs after the server has stopped (measured). The delay and the
  longest request together have to fit inside `ShutdownTimeout`, and that inside the platform's
  stop timeout. On ECS behind an ALB, ECS deregisters the task from its target group before it
  sends SIGTERM (probe 1.4, from the task lifecycle docs), so there the delay is a safety margin. It
  matters most where the signal and the deregistration happen together, as on Kubernetes, and the
  page says so.

The default response writer, plain text `Healthy` or `Unhealthy` with 200 or 503, is what a load
balancer reads, so the page shows that, not a custom JSON writer. A custom writer is named only.

**Alternative considered:** a single `/health` endpoint, Aspire's ServiceDefaults style
(`/health` and `/alive`). The page mentions that ServiceDefaults maps these, and only in
Development, which the probe confirms. The exercise uses the explicit live/ready pair, because that's
what ECS's and a load balancer's settings name.

### D4. Exercise 13 shape

An orders API (`HealthAndShutdown.Starter`, `HealthAndShutdown.Solution`), with:
- an in-memory store the tests can take offline;
- `POST /orders`, which takes a few seconds (it waits on a simulated payment);
- `GET /orders/{id}`;
- the health endpoints.

The four bugs, and their TODOs:
1. **Liveness checks the store.** `/health/live` runs every check, so it fails when the store is
   offline. The fix gives it no checks.
2. **No readiness endpoint.** The fix maps `/health/ready` with the checks tagged `ready`, the store
   among them.
3. **Readiness stays healthy during shutdown.** The fix adds a check, tagged `ready`, that's
   unhealthy once `ApplicationStopping` has fired, and the drain delay (D3) that keeps the app
   answering long enough for that to be seen.
4. **Shutdown drops the in-flight order.** The Starter sets `HostOptions.ShutdownTimeout` shorter
   than an order takes, with a comment that sounds reasonable ("so deploys are quick"). The fix sets
   it above the longest request and below the stop timeout that the container's platform allows,
   and the comment says why.

   The probe confirms that a short timeout really cuts the order off with real Kestrel. If it
   doesn't, the probe finds the realistic bug that does (for example, the order's work taking
   `ApplicationStopping` as its token), records it, and asks Emmz before it changes the TODO.

The payment's delay is fixed in the code, and the tests don't change it (the spec's "without
changing how long an order takes").

### D5. Exercise 13's tests: real Kestrel, in process

`TestServer` doesn't drain requests the way Kestrel does, so a shutdown test against it would test
the wrong thing. The tests start the app on Kestrel, on a random localhost port, in the test process.
.NET 10's `WebApplicationFactory` can do this with `UseKestrel`, and the probe confirmed it (1.2),
with two details:
- **The port:** `UseKestrel(o => o.Listen(IPAddress.Loopback, 0))`. `UseKestrel(0)` listened on the
  default `localhost:5000`, which macOS ControlCenter also holds.
- **The stop:** `IHostApplicationLifetime.StopApplication()`, then wait for `ApplicationStopped`.
  That follows SIGTERM's path. `IHost.StopAsync()` runs the drain delay before
  `ApplicationStopping` fires, so readiness never fails.

The checks, each named with its TODO in its failure message:
- **Liveness (TODO 1):** take the store offline, then expect 200 from `/health/live`.
- **Readiness (TODO 2):** expect 200 from `/health/ready` with the store online, and 503 with it
  offline. A 404 says that the endpoint is missing.
- **Shutdown readiness (TODO 3):** start an order and wait until the store says it's in progress.
  Then start stopping the host, and, during the drain delay, expect 503 from `/health/ready` and
  200 from `/health/live`, while the order is still in flight. A refused connection means there's
  no delay, and the failure message says so.
- **Drain (TODO 4):** start an order, wait until it's in progress, stop the host, then expect the
  order's response to be 201 and the order to be in the store.

They sync on "the order is in progress", which the store signals, never on sleeps, so a slow runner
doesn't race. The store is a singleton the tests replace through `ConfigureServices`.

### D6. Shutdown in a container, measured

The probe publishes exercise 13's Solution with `dotnet publish -t:PublishContainer`, starts an order
against it, and runs `docker stop` (default timeout) while the order is in flight:
- without `--init`: does SIGTERM reach the app as PID 1? .NET registers a SIGTERM handler, so it
  should. Unlike the crash case, this is measured, not assumed;
- with `--init`: the same;
- with the Starter's short shutdown timeout, for contrast.

The page shows the order's response and the app's shutdown log lines, from the real run. It says
how the stop timeout and `ShutdownTimeout` relate: the shutdown timeout has to be the shorter.

### D7. Aspire locally

A scratch AppHost, created and run with the Aspire CLI (Emmz, 2026-11-11), that runs:
- SQL Server (`AddSqlServer(…).AddDatabase(…)`);
- an orders API, the Containers page's Compose API, with `WithReference(db)` and `WaitFor(db)`;
- a second small project that calls the API by its resource name, to show service discovery.

The probe records:
- the exact CLI commands (creating, adding integrations, `aspire run`) and their output;
- the dashboard's address and its sign-in token;
- the connection string's configuration key in the API;
- the SQL Server image Aspire chose;
- the versions (Aspire 13.x).

The page says that the AppHost is for local development, and that production runs on ECS or EKS, as
the page goes on to show. It compares Aspire with Compose in a few lines: Compose is YAML that runs
containers you've built, and Aspire is C# that runs your projects with their dependencies, plus the
dashboard.

**Alternative considered:** a samples project, built by the gates. Emmz chose scratch, so the samples
take no Aspire packages.

### D8. AWS, from the docs

- **ECS on Fargate:** a task definition (JSON) with:
  - the image;
  - `portMappings`;
  - `healthCheck` (a `CMD` that needs a tool in the image, so the page discusses chiseled images
    having no `curl`, and the load balancer's health check as the usual answer);
  - `environment` and `secrets` (from Parameter Store or Secrets Manager);
  - `stopTimeout`;
  - `taskRoleArn` and `executionRoleArn`.

  **The syntax check:** compare the JSON's keys against `aws ecs register-task-definition
  --generate-cli-skeleton`, with `jq`, and run `aws ecs register-task-definition help` and
  `create-service help` for the flags used. No call goes to AWS.
- **EKS:** prose and a table only. It covers:
  - what EKS adds, and costs;
  - the AWS Load Balancer Controller and its target-group health check;
  - EKS Pod Identity for credentials.

  It links to Kubernetes's own docs for probes, mapping liveness and readiness to the page's
  endpoints in words. Any `aws eks` command on the page gets `help` checked.
- **Lambda:** `Amazon.Lambda.AspNetCoreServer.Hosting`'s `AddAWSLambdaHosting` in a scratch project,
  compiled with warnings as errors. Deploying is from the AWS docs (`dotnet lambda deploy-serverless`,
  from `Amazon.Lambda.Tools`, which is what the docs' ASP.NET page uses: probe 1.4), marked as not
  run. The docs spell the enum `LambdaEventSource.HttpAPi`, which doesn't compile, so the page uses
  `HttpApi`. Because `Amazon.Lambda.Tools` isn't installed, that
  command isn't syntax-checked, and the page says so. The page covers cold starts, the 15-minute
  limit, and Native AOT as the cold-start fix (linking back to the Containers page).
- **Configuration:** `Amazon.Extensions.Configuration.SystemsManager`'s `AddSystemsManager`, with a
  path prefix. Secrets Manager is read through Parameter Store's
  `/aws/reference/secretsmanager/` reference, if the probe confirms that the provider supports it,
  or else through the provider the AWS docs name. Each is compiled. The page shows how a parameter
  name maps to a configuration key, and that credentials come from the default chain (the task or
  pod role).

The package versions are the current ones on nuget.org, recorded by the probe. They go only into the
scratch projects, not into `Directory.Packages.props`.

### D9. Tooling rows

- **`aspire`:** Aspire CLI 13.x. The check is `aspire --version`. Install with aspire.dev's
  documented command, which the probe records. It's used for the page and the probe, and no gate
  runs it.
- **`aws`:** AWS CLI v2. The check is `aws --version`, with **no auth check**. Install with AWS's
  installer, or `brew install awscli`. It's used only for syntax checks (`help` and
  `--generate-cli-skeleton`).

Both rows land with §1, as do their `make deps` checks, and the section note says why.

### D10. Section order

1. Probe, versions, and the Aspire and AWS CLI rows (D2, D5, D6, D7, D8, D9).
2. Exercise 13: scaffold, Solution, tests, Starter, CI.
3. The cheatsheet, then the Orchestration & AWS page, and the Containers page's retargeted link.
4. The samples README.
5. The human check.

**§2 lands with `SITE_TEST_EXIT:1` on purpose:** that's the undescribed-exercise check for
`13-health-and-shutdown`. §3 turns it green. The section note and the commit body record it.

### D11. Cheatsheet

`cheatsheets/orchestration-and-aws`, linked both ways with the page:
- health checks: registering, tagging, mapping live and ready, and the probe responses;
- shutdown: the signal path, `ShutdownTimeout`, the stop timeouts, and `--init`;
- the Aspire CLI commands, and an AppHost's shape;
- an ECS task definition's health and stop fields, and the CLI calls;
- ECS, EKS or Lambda, in a table;
- the configuration providers, and where credentials come from.

It has Learn links, and links to aspire.dev and the AWS docs.

### D12. Code accuracy and verification

As in earlier changes:
- every C# snippet is compiled in a scratch project, net10.0, with warnings as errors;
- every command that doesn't need AWS is run as shown, and its output is quoted from that run;
- every AWS command is syntax-checked as D8 says, or marked as not checked;
- "from the exercise" blocks are copies of the real files;
- every URL on both pages is fetched.

## Risks / Trade-offs

- **A shutdown test could race.** → The tests sync on the store's "in progress" signal, never on
  sleeps. Each test owns its own app and store.
- **Real Kestrel on CI could hit port clashes.** → Each app gets a random port (`127.0.0.1:0`), and
  the tests read the port back from the server's addresses.
- **The AWS content isn't run, so it could be wrong in ways a syntax check can't catch.** → It's
  marked, every part links to its AWS doc, and the JSON's keys are checked against the CLI's own
  skeleton. A wrong value (a role's ARN, say) is the reader's to fill in, and the page says so.
- **Aspire changes fast,** and 13.x CLI commands may differ from blog posts and from memory. → The
  page uses only commands the probe ran, with the version, and links to aspire.dev.
- **SQL Server under emulation is slow to start,** so the Aspire run's timing varies. → The page
  quotes no start-up times. It shows the order of events (the database becomes healthy, then the API
  starts).
- **A chiseled image has no `curl` for an ECS container health check.** → The page says so, and
  prefers the load balancer's health check against `/health/ready`. An in-image check needs a tool
  that's in the image (a small health-check binary, or the app itself with an argument), which the
  page names without building one.
- **The PID 1 behaviour on SIGTERM may differ from the crash case.** → D6 measures it before the
  page says anything.
