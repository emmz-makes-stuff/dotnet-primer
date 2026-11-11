# Proposal

## Why

Orchestration & AWS is the last placeholder on the learning path, after Containers. Emmz chose it as
the next change on 2026-11-11. The engineers come from .NET 3.5, where an app ran in IIS on a server
that stayed up for months: IIS recycled the app pool, a load balancer pinged a page, and nobody
thought about what happens to a request when the process stops. Their .NET 10 services will run as
containers on AWS, where tasks start, stop and move all the time. So each service has to say when
it's alive and when it's ready, finish its in-flight work when it's told to stop, run locally with
its dependencies, and take its configuration from AWS. DISCOVERY.md names orchestration (".NET
Aspire") and AWS hosting ("ECS, EKS, Lambda") as an area, and says the cloud guidance is
AWS-specific, because that's where the team deploys.

The Containers page already sends its readers here for the app's own health checks. Its PID 1
Pitfall (an app that crashes as PID 1 never exits) is the other half of the same story: what the
orchestrator sees when the app stops.

## What Changes

- **Write the full Orchestration & AWS page** (`deploy/orchestration-and-aws`), replacing the stub,
  with the three areas Emmz picked on 2026-11-11:
  - **Health checks and graceful shutdown:**
    - ASP.NET Core health checks;
    - liveness against readiness: why liveness mustn't check the database;
    - checking a dependency for readiness;
    - tags, and mapping each endpoint's checks;
    - how SIGTERM reaches the app, `HostOptions.ShutdownTimeout`, and draining in-flight requests;
    - failing readiness once shutdown starts;
    - what `docker stop` does, measured, with and without an init process.
  - **.NET Aspire locally:** an AppHost that runs an API with SQL Server, created and run with the
    **Aspire CLI** (Emmz, 2026-11-11), with the dashboard, references and connection strings,
    service discovery, and health checks. The example is written for the page and run by the probe
    in a scratch folder, like the Containers page's Compose example. It isn't a samples project
    (Emmz, 2026-11-11). The page links to aspire.dev, where Aspire's docs now live.
  - **AWS hosting**, from the AWS docs, **syntax-checked only, and marked as not run** (Emmz,
    2026-11-11), like ECR on the Containers page:
    - ECS on Fargate: a task definition with the container's health check and stop timeout, the
      load balancer's health check, and IAM task roles for credentials;
    - EKS: the AWS side only, meaning the cluster, the load balancer, and pod credentials (EKS Pod
      Identity). It has no Kubernetes manifests: **Kubernetes isn't in scope** (Emmz, 2026-11-11);
    - Lambda: an ASP.NET Core API on Lambda with `AddAWSLambdaHosting`, and when Lambda fits better
      than a container;
    - configuration from Systems Manager Parameter Store and Secrets Manager, through the AWS
      configuration provider, with the C# compiled.
  - **Coming from .NET 3.5:** a short table, after the concepts and before the Pitfalls.
  - **Pitfalls,** then **Exercise 13**.
- **An Orchestration & AWS cheatsheet** (`cheatsheets/orchestration-and-aws`). It's linked both ways
  with the page, listed in the sidebar and on the introduction, and added to the site tests' maps.
- **Exercise 13** (`samples/exercises/13-health-and-shutdown`): a starter, a solution and shared
  tests, like the earlier exercises. The engineer gets a working orders API whose health checks and
  shutdown are wrong, and fixes them (Emmz, 2026-11-11):
  - liveness checks the database, so a database outage would get every instance restarted;
  - there's no readiness check, so traffic arrives before the database can be reached;
  - readiness stays healthy during shutdown, so the load balancer keeps sending requests;
  - shutdown drops an in-flight order.

  The tests run the app in process, with a store they can take offline, so **the exercise needs no
  Docker**.
- **Tooling** (Emmz, 2026-11-11): rows in `docs/dependencies.md`, and checks in `make deps`, for
  the **Aspire CLI** (the page and the probe) and the **AWS CLI** (syntax-checking the page's
  commands only, with no sign-in).
- **The samples README:** the layout and the exercise table gain 13.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" adds the Orchestration & AWS page and its cheatsheet.
  - New: "Orchestration page covers health checks", "… covers graceful shutdown", "… covers .NET
    Aspire locally", "… covers ECS", "… covers EKS and Lambda", "… covers configuration from AWS",
    and "Orchestration page maps .NET 3.5 habits".
- **`site-structure`:** "Cheatsheets per area" grows from thirteen cheatsheets to fourteen.
- **`sample-exercises`:**
  - "Site describes each exercise" gains a scenario for exercise 13.
  - New: "Health exercise separates liveness from readiness", and "Health exercise shuts down
    gracefully".

## Impact

- **Site:**
  - `site/src/content/docs/deploy/orchestration-and-aws.md`, and a new
    `site/src/content/docs/cheatsheets/orchestration-and-aws.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`);
  - the Containers page's pointers to this page, which may be reworded to link the new sections.
- **Samples:**
  - `samples/exercises/13-health-and-shutdown`, and both `.slnx` files;
  - `samples/README.md`.
  - No new packages are expected: health checks are part of ASP.NET Core. If the tests need one,
    it goes in `Directory.Packages.props`.
- **Gates:** `make samples-test` gains exercise 13's tests. They run in process, with no Docker.
- **Tooling:** the Aspire CLI and the AWS CLI join `docs/dependencies.md` and `make deps`, with no
  AWS sign-in. The probe also uses the Aspire templates that the CLI installs, and Docker (already a
  dependency) for SQL Server under Aspire.
- **Out of scope:**
  - Kubernetes: manifests, probes in YAML, a local cluster, and Helm;
  - running anything against a real AWS account: ECS, EKS, Lambda, Parameter Store, Secrets Manager
    and IAM;
  - Terraform or CloudFormation for the AWS resources, beyond naming them;
  - Aspire's deployment publishers (`aspire publish` / `aspire deploy`), beyond naming them;
  - an Aspire project in the samples.
