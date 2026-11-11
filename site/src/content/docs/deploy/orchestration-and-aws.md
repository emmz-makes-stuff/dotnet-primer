---
title: "Orchestration & AWS"
description: "Running .NET services for real: liveness and readiness health checks, graceful shutdown and its timeouts, Aspire on your machine, then Amazon ECS, EKS and Lambda, with configuration and credentials from AWS."
learn:
  - title: "Health checks in ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks
  - title: ".NET Generic Host"
    url: https://learn.microsoft.com/dotnet/core/extensions/generic-host
  - title: "Host and deploy ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/host-and-deploy/
---

By the end of this page you'll be able to give an ASP.NET Core app the two health endpoints an orchestrator needs, and make it stop without dropping work. You'll be able to run an API with its database on your machine with Aspire, and you'll know how the same app runs on Amazon ECS, on EKS or on Lambda, and where it gets its settings and AWS credentials there.

:::tip[Cheatsheet]
Keep the [Orchestration & AWS cheatsheet](/cheatsheets/orchestration-and-aws/) open while you work.
:::

The health and shutdown examples are the orders API from [exercise 13](#exercise-13-health-checks-and-graceful-shutdown). Everything that doesn't need AWS was measured on one machine: an Apple silicon Mac, with SDK 10.0.404 and runtime 10.0.15, Docker on OrbStack, and Aspire 13, on 11 November 2026. Nothing on this page was run against AWS: the AWS parts come from the AWS documentation, linked from each section, and their commands and JSON were checked against the AWS CLI's own syntax.

## Health checks

An orchestrator asks your app two different questions, and each needs its own endpoint:

- **Liveness: is the process working?** If not, the orchestrator kills the container and starts a new one. Only check what a restart would fix: if the app can answer at all, it's alive.
- **Readiness: can it take traffic right now?** If not, the load balancer stops sending it requests, but leaves it running until it's ready again. This is where the database, a queue or anything else the app needs gets checked.

**Liveness mustn't check a shared dependency such as the database.** When the database goes down, every instance's liveness would fail at once, and the orchestrator would restart all of them. A restart can't bring the database back, so you'd get an outage plus a wave of restarts, and instances that are busy starting up when the database returns.

Each check is a class, or a lambda, registered with tags. Each endpoint picks the checks it runs with a predicate:

```csharp title="Program.cs (from the exercise)"
builder.Services.AddHealthChecks()
    // TODO 2 (solved): tagged "ready", so the readiness endpoint runs it, and liveness doesn't.
    .AddCheck<StoreHealthCheck>("store", tags: ["ready"])
    // TODO 3 (solved): readiness fails once the app is told to stop.
    .AddCheck<ShuttingDownHealthCheck>("shutting-down", tags: ["ready"]);
```

```csharp title="Program.cs (from the exercise)"
// TODO 1 (solved): liveness runs no checks. If the app can answer, it's alive, and restarting it
// wouldn't bring the store back.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// TODO 2 (solved): readiness runs the checks tagged "ready": can the app take traffic right now?
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
```

```csharp title="Health.cs (from the exercise)"
/// <summary>Unhealthy while the store is offline: the app can't take orders, so it isn't ready.</summary>
public sealed class StoreHealthCheck(OrderStore store) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Online
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The order store is offline."));
}
```

`HealthCheckOptions` is in `Microsoft.AspNetCore.Diagnostics.HealthChecks`. Liveness runs no checks at all, and readiness runs the two tagged `ready`. The exercise's order store stands in for a database, and `Store:Online=false` starts it offline. With the exercise's image (built [below](#docker-stop-with-an-order-in-flight)) started with `-e Store__Online=false`, the endpoints answered:

```text
GET /health/live  -> 200 Healthy
GET /health/ready -> 503 Unhealthy
POST /orders      -> 503 {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.4","title":"Service Unavailable","status":503,"detail":"The order store is offline."}
```

The app logged the failing check at `fail:` level, with its message:

```text
fail: Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService[103]
      Health check store with status Unhealthy completed after 0.3716ms with message 'The order store is offline.'
```

The default response is plain text, which is all a load balancer reads: `200 Healthy`, `200 Degraded` (also measured: degraded still counts as up), or `503 Unhealthy`. You can pass your own `ResponseWriter` in `HealthCheckOptions` to return JSON for people, but keep the status codes. If you use Aspire's ServiceDefaults, its `MapDefaultEndpoints` maps `/health` and `/alive` instead, and only in Development: see [Aspire](#aspire-on-your-machine).

## Graceful shutdown

When an orchestrator replaces a container, for a deployment, a scale-in or a failed liveness check, it asks the app to stop first:

1. The platform sends `SIGTERM` to the container's first process, PID 1. .NET handles it, even as PID 1 with no init process, and fires `IHostApplicationLifetime.ApplicationStopping`. The log says `Application is shutting down...`.
2. The host stops its services. Kestrel stops taking new connections at once, and waits for the requests it has, up to `HostOptions.ShutdownTimeout`, which is **30 s** by default in .NET 10. Requests still running after that are cut off.
3. The app exits with code 0.
4. If it's still running when the platform's **stop timeout** runs out, the platform sends `SIGKILL`, and the container exits with code 137. `docker stop` waits **10 s** (measured: 10.24 s from `docker stop` to the kill), ECS waits 30 s by default, and Kubernetes waits 30 s.

So the two have to nest: the longest request has to finish inside `ShutdownTimeout`, and `ShutdownTimeout` has to end before the platform's stop timeout.

```csharp title="Program.cs (from the exercise)"
// TODO 4 (solved): the shutdown timeout covers the drain delay (2 s) and the longest request, an
// order (3 s), with room to spare. It has to stay under the platform's stop timeout, after which
// the app is killed: 10 s for `docker stop`, and 30 s by default on ECS.
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(8));
```

### Failing readiness when shutdown starts

Once the app is stopping, it shouldn't get new requests: readiness should fail. A check tagged `ready` does that:

```csharp title="Health.cs (from the exercise)"
/// <summary>
/// TODO 3 (solved): unhealthy once the app has been told to stop, so the load balancer stops sending
/// it new requests while it finishes the ones it has.
/// </summary>
public sealed class ShuttingDownHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(lifetime.ApplicationStopping.IsCancellationRequested
            ? HealthCheckResult.Unhealthy("The app is shutting down.")
            : HealthCheckResult.Healthy());
}
```

On its own, though, **nobody ever sees it fail**. Measured: about 0.3 s after `SIGTERM`, Kestrel refused every new connection, to `/health/ready` as much as anything else, so no load balancer could ask. The app has to keep answering for a moment after the signal: a **drain delay**. An `IHostedLifecycleService` gets a `StoppingAsync` call, which runs before any hosted service's `StopAsync`, and so before the server stops. A delay in a plain `StopAsync` comes too late: measured, it ran after Kestrel had already stopped.

```csharp title="Health.cs (from the exercise)"
/// <summary>
/// TODO 3 (solved): keeps the server answering for a moment after the stop signal, so that the
/// load balancer sees readiness fail before the app stops taking connections. Without it, Kestrel
/// refuses new connections at once, and nobody ever sees the shutting-down check.
/// <c>StoppingAsync</c> runs before any hosted service's <c>StopAsync</c>, so before the server
/// stops. A delay in <c>StopAsync</c> would come too late.
/// </summary>
public sealed partial class DrainDelay(ILogger<DrainDelay> logger) : IHostedLifecycleService
{
    /// <summary>
    /// In production, make this a few of the load balancer's health check intervals. It's short
    /// here, so that the tests are quick.
    /// </summary>
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(2);

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        LogDraining(Duration);
        try
        {
            await Task.Delay(Duration, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The shutdown timeout ran out first. Stop waiting, but don't throw: an exception here
            // fails the host's StopAsync, and the app exits with an error.
            LogDrainCutShort();
        }
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Draining for {Delay} before the server stops")]
    private partial void LogDraining(TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The shutdown timeout ran out during the drain delay")]
    private partial void LogDrainCutShort();
}
```

Register it with `builder.Services.AddHostedService<DrainDelay>();`. The delay counts against `ShutdownTimeout`, so it goes in the sum: drain delay, plus the longest request, has to be less than `ShutdownTimeout`. In production, make it a few of the load balancer's health check intervals. The exercise uses 2 s, to keep its tests quick.

How much the delay matters depends on the platform. **ECS behind a load balancer takes the task out of the target group first** (the task's `DEACTIVATING` state), and only then sends `SIGTERM` (`STOPPING`), according to the [task lifecycle](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task-lifecycle-explanation.html). There, the delay is a safety margin. **On Kubernetes, the two happen at the same time**: "At the same time as the kubelet is starting graceful shutdown of the Pod, the control plane evaluates whether to remove that shutting-down Pod from EndpointSlice objects", in the [pod lifecycle](https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#pod-termination). There, without a delay, requests still arrive at a server that has stopped listening.

### `docker stop` with an order in flight

The exercise's Solution, built into an image without changing its project, and run:

```sh
dotnet publish -t:PublishContainer -p:ContainerRepository=health-orders -p:ContainerFamily=noble-chiseled -p:InvariantGlobalization=true
docker run -d --name ex13 -p 8480:8080 health-orders
```

Port 8080 was taken on this machine, so the host's side of the mapping is 8480: the container's side stays 8080. An order takes 3 s. This is what a client saw, starting an order, running `docker stop ex13` 0.75 s later, and asking both endpoints every half second:

```text
[  0.11s] live   200 Healthy
[  0.12s] ready  200 Healthy
[  0.87s] stop   docker stop ex13
[  1.14s] live 200 Healthy | ready 503 Unhealthy
[  1.67s] live 200 Healthy | ready 503 Unhealthy
[  2.17s] live 200 Healthy | ready 503 Unhealthy
[  2.67s] live 200 Healthy | ready 503 Unhealthy
[  3.15s] order  201 {"id":1,"sku":"SKU-1"}
[  3.18s] live ERR ECONNRESET | ready ERR ECONNRESET
[  3.34s] stop out ex13
[  3.34s] stop exit 0
[  3.68s] live ERR ECONNREFUSED | ready ERR ECONNREFUSED
```

**The order completed**, after the stop. For the 2 s of the drain delay, readiness failed and liveness didn't. Then the server stopped taking connections: while the container was still there, Docker's port mapping reset them (`ECONNRESET`), and once it had gone, they were refused. `docker stop` returned once the app had exited, and `docker inspect ex13 --format '{{.State.ExitCode}}'` gave `0`. The app logged:

```text
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
info: HealthOrders.DrainDelay[2032334755]
      Draining for 00:00:02 before the server stops
fail: Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService[103]
      Health check shutting-down with status Unhealthy completed after 0.0298ms with message 'The app is shutting down.'
```

The last entry came once for each readiness request. Runs with `docker run --init` behaved the same: unlike a [crash](/deploy/containers/#pitfalls), a stop doesn't need an init process.

### When the timeouts are wrong

Measured with a 5 s request, on a scratch app:

| What's wrong                                          | What happened                                                                                                  | Change                                                                                |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `ShutdownTimeout` (1 s) shorter than the request      | At 1 s, the request's connection was dropped, and the order was never stored. **The app still exited with 0.** | Raise `ShutdownTimeout` above the longest request                                     |
| The platform's stop timeout shorter than the shutdown | `docker stop` killed the app at 10 s, partway through, and the container exited with **137**                   | Raise the stop timeout (`docker stop -t`, ECS `stopTimeout`), or shorten the shutdown |

The first is the dangerous one, because nothing looks wrong: exit code 0, and no error logged. Only the client knows its order was lost.

## Aspire on your machine

[Aspire](https://aspire.dev/) (it used to be .NET Aspire) runs your projects together on your machine, with the containers they need, from a C# file called the AppHost. It also gives you a dashboard of their logs, traces and metrics. Compose, from the [Containers page](/deploy/containers/#compose-locally), runs images you've built, from YAML. Aspire runs your projects straight from source, starts their dependencies, and passes each one its connection strings and the addresses of the others. **The AppHost is for local development, not a production deployment**: in production, the same projects run as containers on ECS or EKS, below.

With the Aspire CLI (`curl -sSL https://aspire.dev/install.sh | bash`, or `irm https://aspire.dev/install.ps1 | iex` on Windows):

```sh
aspire new aspire-empty --language csharp -n Shop -o shop
cd shop
aspire add sqlserver
```

```text
✅ Created C# (.NET) project at …/shop
✅ The package Aspire.Hosting.SqlServer::13.6.5 was added successfully.
```

`aspire new` asks two questions: whether to use `*.dev.localhost` URLs, and whether to configure AI agent environments. This page answered no to both, which `--localhost-tld false --suppress-agent-init` does without asking.

The AppHost is a single file, `apphost.cs`, with no project file. Next to it, `Orders.Api` is the Containers page's Compose API, unchanged: it still reads `GetConnectionString("Orders")`. `Shopfront` is a small web app that calls it.

```csharp title="apphost.cs"
#:package Aspire.Hosting.SqlServer@13.6.5
#:sdk Aspire.AppHost.Sdk@13.6.5
#:property AspireUseCliBundle=true

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql");
var db = sql.AddDatabase("Orders");

var api = builder.AddProject("api", "Orders.Api/Orders.Api.csproj")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject("shopfront", "Shopfront/Shopfront.csproj")
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
```

- `AddSqlServer` runs SQL Server in a container. Aspire chose `mcr.microsoft.com/mssql/server:2022-latest`, with a generated `sa` password, and a fresh container each run, so the data doesn't last. On Apple silicon it runs under emulation, as on the [Docker page](/platform/docker/#macos).
- `WithReference(db)` gives the API **`ConnectionStrings__Orders`**, named after the database: `Server=127.0.0.1,<port>;User ID=sa;Password=<generated>;TrustServerCertificate=true;Initial Catalog=Orders`. That's why the Compose API needed no change.
- `WaitFor(db)` holds the API back until SQL Server is healthy, as `depends_on` with `condition: service_healthy` does in Compose.
- `WithReference(api)` gives the shopfront `services__api__http__0=http://localhost:5380`, the API's address. With ServiceDefaults' service discovery, the shopfront calls `http://api`:

```csharp title="Shopfront/Program.cs"
builder.AddServiceDefaults();

// "api" is the API's resource name in the AppHost; service discovery resolves it.
builder.Services.AddHttpClient("orders", client => client.BaseAddress = new Uri("http://api"));

var app = builder.Build();

// /health and /alive: the AppHost's WithHttpHealthCheck("/health") asks the first.
app.MapDefaultEndpoints();
```

`AddServiceDefaults` and `MapDefaultEndpoints` come from the ServiceDefaults project that `aspire new aspire-starter` creates: copy it from there into an empty AppHost's folder. Then run it all:

```sh
aspire run
```

```text
     AppHost:  apphost.cs
   Dashboard:  https://localhost:17063/login?t=<token>
        Logs:  ~/.aspire/logs/cli_20261111T190316_e29b8e7d.log
               Press CTRL+C to stop the AppHost and exit.
```

**Open the Dashboard link** to sign in: the token in it is new each run. In another terminal, `aspire describe` lists what's running:

```text
┌───────────┬───────────────────────────┬─────────┬─────────┬───────────────────────┐
│ Name      │ Type                      │ State   │ Health  │ URLs                  │
├───────────┼───────────────────────────┼─────────┼─────────┼───────────────────────┤
│ api       │ Project                   │ Running │ Healthy │ http://localhost:5380 │
│ Orders    │ SqlServerDatabaseResource │ Running │ Healthy │ -                     │
│ shopfront │ Project                   │ Running │ Healthy │ http://localhost:5381 │
│ sql       │ Container                 │ Running │ Healthy │ tcp://localhost:60757 │
└───────────┴───────────────────────────┴─────────┴─────────┴───────────────────────┘
```

Through the shopfront, an order went to the API by name and into SQL Server: `201 {"id":1,"sku":"SKU-1"}`, and then `GET /orders` gave `[{"id":1,"sku":"SKU-1"}]`. `aspire wait shopfront` waits until a resource is healthy, and `aspire stop` stops it all and removes the SQL Server container.

- **A project needs an endpoint in `Properties/launchSettings.json`.** `dotnet new web` makes one. Without it, `aspire run` failed with `Could not create HTTP health check for resource 'shopfront' as no endpoint was found matching one of the specified names: https, http`.
- **ServiceDefaults' health endpoints exist in Development only.** Its `MapDefaultEndpoints` maps `/health` (every check) and `/alive` (checks tagged `live`) inside `if (app.Environment.IsDevelopment())`. Measured: both answered `200 Healthy` under Aspire, and `404` with `ASPNETCORE_ENVIRONMENT=Production`. In production, map your own pair, as [above](#health-checks).

## Amazon ECS on Fargate

ECS runs your containers as **tasks**, from a **task definition**: the image, its port, its settings, and how to check and stop it. A **service** keeps a number of tasks running behind a load balancer, and replaces them on a deployment. Fargate runs them without servers for you to manage.

**This wasn't run for this page.** It's from the [task definition parameters](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_definition_parameters.html), and its keys were checked against `aws ecs register-task-definition --generate-cli-skeleton`.

```json title="task-definition.json"
{
  "family": "orders-api",
  "requiresCompatibilities": ["FARGATE"],
  "networkMode": "awsvpc",
  "cpu": "512",
  "memory": "1024",
  "runtimePlatform": {
    "operatingSystemFamily": "LINUX",
    "cpuArchitecture": "ARM64"
  },
  "executionRoleArn": "arn:aws:iam::123456789012:role/orders-api-execution",
  "taskRoleArn": "arn:aws:iam::123456789012:role/orders-api-task",
  "containerDefinitions": [
    {
      "name": "api",
      "image": "123456789012.dkr.ecr.eu-west-2.amazonaws.com/orders-api:1.0.0",
      "essential": true,
      "portMappings": [{ "containerPort": 8080, "protocol": "tcp" }],
      "environment": [{ "name": "Warehouse__Name", "value": "Leeds" }],
      "secrets": [
        {
          "name": "ConnectionStrings__Orders",
          "valueFrom": "arn:aws:secretsmanager:eu-west-2:123456789012:secret:orders-api/db-AbCdEf"
        }
      ],
      "stopTimeout": 45,
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/orders-api",
          "awslogs-region": "eu-west-2",
          "awslogs-stream-prefix": "api"
        }
      }
    }
  ]
}
```

That task definition has no container health check, so it's safe with the chiseled image from the [Containers page](/deploy/containers/), which has no shell and no `curl`. With an image that has both, you can add one to the container definition, next to `stopTimeout`:

```json title="healthCheck (needs a shell and curl in the image)"
{
  "healthCheck": {
    "command": [
      "CMD-SHELL",
      "curl -f http://localhost:8080/health/live || exit 1"
    ],
    "interval": 30,
    "timeout": 5,
    "retries": 3,
    "startPeriod": 10
  }
}
```

- **`image` and `portMappings`:** the image you [pushed to ECR](/deploy/containers/#amazon-ecr), and the port it listens on, 8080. `runtimePlatform` says `ARM64` because an Apple silicon Mac builds `linux/arm64` images: match it to your image.
- **`environment` and `secrets`:** `environment` sets plain variables. Each `secrets` entry becomes a variable too, read from [Secrets Manager](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/secrets-envvar-secrets-manager.html) or [Parameter Store](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/secrets-envvar-ssm-paramstore.html) when the task starts. `ConnectionStrings__Orders` reaches the app as `ConnectionStrings:Orders`, as on the [Containers page](/deploy/containers/#configuration-through-the-environment).
- **`healthCheck`:** the container's [health check](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/healthcheck.html), pointed at liveness: a [service](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/ecs_services.html) replaces a task that fails it. Its defaults are an interval of 30 s, a timeout of 5 s and 3 retries. **It runs inside the container**, so it needs `curl` there, and `CMD-SHELL` needs a shell. A chiseled image has neither: with one, leave `healthCheck` out, as the task definition above does, and rely on the load balancer's check below.
- **`stopTimeout`:** how long ECS waits after `SIGTERM` before it sends `SIGKILL`. It's 30 s if you leave it out, and at most 120 s on Fargate. 45 s leaves room above a 30 s `ShutdownTimeout`.
- **`taskRoleArn` and `executionRoleArn`:** the [task role](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task-iam-roles.html) is what your code's AWS calls run as: see [credentials](#configuration-and-credentials-from-aws). The [execution role](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_execution_IAM_role.html) is for ECS itself: pulling the image, writing the logs, and reading the `secrets` (it needs `secretsmanager:GetSecretValue` or `ssm:GetParameters`).

**The load balancer's health check is the one that matters for traffic.** An Application Load Balancer checks each task through its target group, and by default asks for `/`, every 30 s, with a 5 s timeout. A target is out after 2 failures and back after 5 successes, and only `200` counts ([target group health checks](https://docs.aws.amazon.com/elasticloadbalancing/latest/application/target-group-health-checks.html)). **Point it at `/health/ready`.** When ECS stops a task, it takes the task out of the target group first, and the load balancer then waits up to its **deregistration delay** for requests in flight: 300 s by default ([`deregistration_delay.timeout_seconds`](https://docs.aws.amazon.com/elasticloadbalancing/latest/application/edit-target-group-attributes.html#deregistration-delay)).

```sh
aws ecs register-task-definition --cli-input-json file://task-definition.json

aws elbv2 create-target-group --name orders-api --protocol HTTP --port 8080 --target-type ip \
  --vpc-id vpc-0abc --health-check-path /health/ready --matcher HttpCode=200

aws ecs create-service --cluster shop --service-name orders-api --task-definition orders-api \
  --desired-count 2 --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-0abc],securityGroups=[sg-0abc]}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:eu-west-2:123456789012:targetgroup/orders-api/0abc,containerName=api,containerPort=8080" \
  --health-check-grace-period-seconds 30
```

These weren't run: their syntax was checked with `aws … help`, and by the AWS CLI parsing their arguments with no credentials. `--target-type ip` is what Fargate needs, and `--health-check-grace-period-seconds` gives a new task time to start before its failed checks count. To deploy a new image, register a new revision of the task definition, then `aws ecs update-service --cluster shop --service orders-api --task-definition orders-api`. ECS starts new tasks, waits for them to pass the load balancer's check, and then stops the old ones, through the shutdown above.

## Amazon EKS

EKS runs Kubernetes for you. It fits when your team already runs Kubernetes, or runs many services that share a cluster and its tooling. It costs **$0.10 per cluster per hour** on top of what runs in it, $0.60 for a Kubernetes version in extended support ([EKS pricing](https://aws.amazon.com/eks/pricing/)), and it means learning and running Kubernetes itself. **This wasn't run for this page.** It covers only the AWS side: for Kubernetes itself, start with [its documentation](https://kubernetes.io/docs/home/).

| On Kubernetes                                                                                                          | For the orders API                                                                                                                                                                                                                                                                                                                            |
| ---------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| A liveness probe                                                                                                       | `httpGet` on `/health/live`: the kubelet restarts the container when it fails ([probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/))                                                                                                                                               |
| A readiness probe                                                                                                      | `httpGet` on `/health/ready`: the pod leaves its Service's endpoints when it fails                                                                                                                                                                                                                                                            |
| `terminationGracePeriodSeconds`                                                                                        | The stop timeout: 30 s by default. Keep `ShutdownTimeout` under it, and keep the [drain delay](#failing-readiness-when-shutdown-starts): the pod leaves the endpoints at the same time as it gets `SIGTERM`                                                                                                                                   |
| The [AWS Load Balancer Controller](https://docs.aws.amazon.com/eks/latest/userguide/aws-load-balancer-controller.html) | Creates an ALB for an `Ingress`, and an NLB for a `Service` of type `LoadBalancer`. Its target group's health check is set by [annotations](https://kubernetes-sigs.github.io/aws-load-balancer-controller/latest/guide/ingress/annotations/#health-check), such as `alb.ingress.kubernetes.io/healthcheck-path`: point it at `/health/ready` |
| [EKS Pod Identity](https://docs.aws.amazon.com/eks/latest/userguide/pod-identities.html)                               | Gives a pod an IAM role, through its service account: EKS sets the variables that the AWS SDK reads                                                                                                                                                                                                                                           |

```sh
aws eks create-pod-identity-association --cluster-name shop --namespace orders \
  --service-account orders-api --role-arn arn:aws:iam::123456789012:role/orders-api-pod
```

That associates a role with the `orders-api` service account in the `orders` namespace. Its syntax was checked with `aws eks create-pod-identity-association help`. The EKS Pod Identity Agent has to run on the cluster's nodes, because it's what hands out the credentials: see the link above.

## AWS Lambda

Lambda runs your code per request, and scales to zero when nothing comes in. An ASP.NET Core API can run on it unchanged, apart from one line: `Amazon.Lambda.AspNetCoreServer.Hosting` (2.2.1) swaps Kestrel for a host that turns Lambda's events into requests. Run locally, Kestrel serves as usual.

```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.MapGet("/orders/{id:int}", (int id) => Results.Ok(new { id, sku = "SKU-1" }));

await app.RunAsync();
```

`LambdaEventSource` says what calls the function: `HttpApi` for an API Gateway HTTP API, `RestApi` for a REST API, or `ApplicationLoadBalancer`. (The [AWS page](https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-asp.html) spells it `HttpAPi`, which doesn't compile.) The code above compiles, but **wasn't deployed for this page**. To deploy, the AWS docs start from a template, and deploy with the `Amazon.Lambda.Tools` .NET tool, which creates the function and its API through CloudFormation:

```sh
dotnet tool install -g Amazon.Lambda.Tools
dotnet new serverless.AspNetCoreMinimalAPI -n MinimalApiOnLambda
dotnet lambda deploy-serverless
```

These weren't run or syntax-checked, because `Amazon.Lambda.Tools` isn't installed here.

- **Each request can run for at most 15 minutes** (900 s, from the [Lambda quotas](https://docs.aws.amazon.com/lambda/latest/dg/gettingstarted-limits.html)). Long-running work belongs in a queue and a worker, not a request.
- **The first request after a quiet spell is slow**: a cold start, while Lambda starts a new instance and .NET JIT-compiles your code. [Native AOT](/deploy/containers/#native-aot) cuts it, if your code and its libraries support trimming. AWS's [Native AOT page](https://docs.aws.amazon.com/lambda/latest/dg/dotnet-native-aot.html) covers it.
- Lambda has a managed **`dotnet10`** runtime ([runtimes](https://docs.aws.amazon.com/lambda/latest/dg/lambda-runtimes.html)). There's no container to stop, so health endpoints and graceful shutdown don't apply in the same way.

## ECS, EKS or Lambda?

| Service            | Suits                                                                                         | Trade-off                                                                                     |
| ------------------ | --------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| **ECS on Fargate** | Containerized APIs and workers, when you want AWS to run the platform                         | AWS-specific, with fewer knobs than Kubernetes. You pay for tasks while they run, busy or not |
| **EKS**            | A team already on Kubernetes, or many services sharing a cluster, its tooling and its add-ons | $0.10 per cluster per hour, plus Kubernetes to learn, upgrade and run                         |
| **Lambda**         | Spiky or light traffic, event handlers, and anything that can scale to zero                   | 15 minutes at most per request, cold starts, and no long-lived connections or background work |

## Configuration and credentials from AWS

On ECS, the task definition's `secrets` turn into environment variables at start-up. The app can also read [Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html) itself, as a configuration provider, with `Amazon.Extensions.Configuration.SystemsManager` (7.1.1):

```csharp title="Program.cs"
// Every parameter under /orders-api/ becomes a key: /orders-api/Warehouse/Name -> Warehouse:Name.
builder.Configuration.AddSystemsManager("/orders-api/");

// A Secrets Manager secret, through Parameter Store's reserved path. A JSON secret's keys become
// configuration keys, so a secret {"ConnectionStrings:Orders": "..."} fills ConnectionStrings:Orders.
builder.Configuration.AddSystemsManager("/aws/reference/secretsmanager/orders-api/db");
```

The provider strips the path you give it, and turns each remaining `/` into `:`. So `/orders-api/Warehouse/Name` arrives as `Warehouse:Name`, and binds to the same options as `appsettings.json` would ([the provider's README](https://github.com/aws/aws-dotnet-extensions-configuration)). Parameter Store reads a [Secrets Manager secret](https://docs.aws.amazon.com/systems-manager/latest/userguide/integration-ps-secretsmanager.html) through the reserved path `/aws/reference/secretsmanager/`. Sources added later win, so these override `appsettings.json`. The role needs `ssm:GetParametersByPath` for the path. The secret's reference path is read with one `GetParameter` call, which the provider's [source](https://github.com/aws/aws-dotnet-extensions-configuration/blob/main/src/Amazon.Extensions.Configuration.SystemsManager/Internal/SystemsManagerProcessor.cs) shows, so it needs `ssm:GetParameter`. Parameter Store only passes the call through, and the reference fails without permission to read the secret itself, so the role needs `secretsmanager:GetSecretValue` on it too. **This compiles, but wasn't run against AWS.**

**The app gets its AWS credentials from its role, never from keys in its configuration.** The AWS SDK looks for them by itself, and on each platform finds a role:

| Runs on      | Credentials come from                                                |
| ------------ | -------------------------------------------------------------------- |
| ECS          | The task role, `taskRoleArn`                                         |
| EKS          | EKS Pod Identity: the role associated with the pod's service account |
| Lambda       | The function's execution role                                        |
| Your machine | Your own sign-in, such as `aws login` or an SSO profile              |

The credentials are short-lived, and AWS rotates them for you. Access keys in `appsettings.json`, an environment variable or the image would last until someone notices, and would leak with the image or the repo.

## Coming from .NET 3.5

| In .NET 3.5                                                      | Now                                                                                                                                      |
| ---------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| The load balancer pinging a page, such as `ping.aspx`            | The [readiness endpoint](#health-checks), `/health/ready`, which checks what the app needs, plus a liveness endpoint that checks nothing |
| IIS recycling the app pool when it misbehaves, or on a schedule  | The orchestrator replacing a container whose [liveness](#health-checks) check fails, after a [graceful shutdown](#graceful-shutdown)     |
| Changing settings on the server, in `web.config` or IIS Manager  | [Parameter Store, Secrets Manager](#configuration-and-credentials-from-aws) or the task definition's environment, then a new deployment  |
| A deployment that takes the site offline, with `app_offline.htm` | A rolling deployment: new tasks pass readiness first, and old ones [drain](#failing-readiness-when-shutdown-starts) before they stop     |

## Pitfalls

- **Liveness that checks the database.** When the database goes down, every instance fails liveness, and the orchestrator restarts them all, which can't fix it. Fix: liveness runs no checks, and the database check goes in readiness.
- **Readiness that stays healthy during shutdown.** The load balancer keeps sending requests to an app that's about to stop taking connections. Fix: a check, tagged `ready`, that fails once `ApplicationStopping` fires, and a [drain delay](#failing-readiness-when-shutdown-starts) so that someone sees it.
- **A `ShutdownTimeout` shorter than the longest request.** Requests still running are cut off, and the app exits with 0, so nothing looks wrong. Fix: raise it above the longest request, plus the drain delay.
- **A `ShutdownTimeout` longer than the platform's stop timeout.** The platform kills the app partway through its shutdown, with exit code 137. Fix: keep `ShutdownTimeout` below `docker stop`'s 10 s, ECS's `stopTimeout` or Kubernetes's `terminationGracePeriodSeconds`, or raise those.
- **Request work that takes `ApplicationStopping` as its token.** It's cancelled the moment the stop arrives, and measured, the request failed with `500` straight away. Fix: pass the request's own token, and let the shutdown timeout bound it.
- **A drain delay that throws when the shutdown timeout runs out.** `Task.Delay` with `StoppingAsync`'s token throws when the token is cancelled, and that fails the host's stop, so the app exits with an error. Fix: catch `OperationCanceledException` when that token is cancelled, as [above](#failing-readiness-when-shutdown-starts).
- **An ECS container health check that needs `curl` in a chiseled image.** The command runs inside the container, which has no `curl` and no shell, so the task is marked unhealthy and replaced, over and over. Fix: leave the container's `healthCheck` out, and let the load balancer check `/health/ready`.
- **AWS access keys in configuration or the image.** They don't expire, and they leak with the image or the repo. Fix: a [task or pod role](#configuration-and-credentials-from-aws), which the SDK finds by itself.
- **Treating the Aspire AppHost as a production deployment.** It's built for your machine: a fresh database each run, generated passwords, and everything on one host. Fix: deploy the projects as containers, to ECS or EKS.

## Exercise 13: health checks and graceful shutdown

**Goal:** give an orders API the health endpoints an orchestrator needs, and make it finish an order that's in flight when it's told to stop. Its liveness endpoint checks the order store, it has no readiness endpoint, and its shutdown timeout is shorter than an order takes. The endpoints, the store and the slow payment are already written.

**Where:** [`samples/exercises/13-health-and-shutdown`](https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/13-health-and-shutdown) in the GitHub repo. Clone the repo, then work in the `Starter` project. **It needs no Docker**: the store is in memory, and the tests run the app on Kestrel inside the test process.

**Steps:**

1. In `Starter/Program.cs`, give `/health/live` no checks, so that it stays healthy when the store is offline (TODO 1).
2. In the same file, tag the store's check `ready`, and map `/health/ready` to the checks tagged `ready` (TODO 2).
3. In `Starter/Health.cs`, write a check that's unhealthy once `ApplicationStopping` has fired, and a drain delay in an `IHostedLifecycleService`'s `StoppingAsync`. Register both in `Program.cs`, the check tagged `ready` (TODO 3).
4. In `Starter/Program.cs`, set `HostOptions.ShutdownTimeout` above the drain delay plus the longest request, an order's 3 s, and below the platform's stop timeout, and say why in the comment (TODO 4).

**Check your work:** the tests start red and turn green as you go.

```sh
dotnet test --project samples/exercises/13-health-and-shutdown/Starter.Tests
```

Each failure names what's wrong and the TODO to look at. The shutdown tests start an order, wait until the store says it's in progress, and then stop the app as `SIGTERM` would. Then they check readiness during the drain, and that the order completes and is stored. TODO 3's test needs the readiness endpoint from TODO 2 first, and says so.

**Try it yourself:** from `samples/exercises/13-health-and-shutdown/Starter`, run `dotnet run -- --Store:Online=false`, and open `/health/live`, on `http://localhost:5000`, and `/health/ready` before and after TODOs 1 and 2. Then build the image as in [`docker stop` with an order in flight](#docker-stop-with-an-order-in-flight), and stop it during an order, before and after TODO 4.

Stuck? Compare with the `Solution` project next to it. The exercises are for practice only, and nothing is recorded or submitted.
