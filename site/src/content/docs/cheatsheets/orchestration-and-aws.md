---
title: Orchestration & AWS cheatsheet
description: Health checks for liveness and readiness, graceful shutdown and its timeouts, the Aspire CLI and an AppHost, an ECS task definition's health and stop fields, ECS or EKS or Lambda, and configuration and credentials from AWS, on one page.
prev: false
next: false
learn:
  - title: Health checks in ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks
  - title: .NET Generic Host (shutdown)
    url: https://learn.microsoft.com/dotnet/core/extensions/generic-host
  - title: Host and deploy ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/host-and-deploy/
---

Back to [Orchestration & AWS](/deploy/orchestration-and-aws/).

## Health checks

```csharp title="Program.cs"
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

builder.Services.AddHealthChecks()
    .AddCheck<StoreHealthCheck>("store", tags: ["ready"])
    .AddCheck<ShuttingDownHealthCheck>("shutting-down", tags: ["ready"]);

var app = builder.Build();

// Liveness: no checks. If the app can answer, it's alive.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// Readiness: the checks tagged "ready". Can it take traffic right now?
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
```

```csharp title="A check"
public sealed class StoreHealthCheck(OrderStore store) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Online
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The order store is offline."));
}
```

| Endpoint        | Runs                      | Fails when                                   | Who acts on it                              |
| --------------- | ------------------------- | -------------------------------------------- | ------------------------------------------- |
| `/health/live`  | nothing                   | the app can't answer at all                  | the orchestrator restarts the container     |
| `/health/ready` | the `ready`-tagged checks | a dependency is down, or the app is stopping | the load balancer stops sending it requests |

The default writer answers plain text: `200 Healthy`, or `503 Unhealthy`. `Degraded` answers `200 Degraded`.

## Shutdown

```text
stop request → SIGTERM → ApplicationStopping fires → readiness fails
             → drain delay (StoppingAsync) → Kestrel stops taking connections
             → in-flight requests finish, up to ShutdownTimeout → exit 0
             → still running at the stop timeout? SIGKILL, exit 137
```

| Timeout                       | Default             | Set with                                                              |
| ----------------------------- | ------------------- | --------------------------------------------------------------------- |
| `HostOptions.ShutdownTimeout` | 30 s                | `builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = …)` |
| `docker stop`                 | 10 s                | `docker stop -t 30`, or `stop_grace_period` in Compose                |
| ECS `stopTimeout` (Fargate)   | 30 s, at most 120 s | the container definition                                              |
| ALB deregistration delay      | 300 s               | `deregistration_delay.timeout_seconds`                                |

Drain delay + longest request < `ShutdownTimeout` < the platform's stop timeout.

```csharp title="A drain delay"
public sealed class DrainDelay : IHostedLifecycleService
{
    // StoppingAsync runs before any hosted service's StopAsync, so before the server stops.
    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The shutdown timeout ran out. Don't throw, or the host's StopAsync fails.
        }
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// builder.Services.AddHostedService<DrainDelay>();
```

- .NET handles SIGTERM as PID 1, so `--init` makes no difference to a stop. It matters for a crash: see [Containers](/deploy/containers/#pitfalls).
- Don't pass `ApplicationStopping` to a request's work: it cancels at once. Use the request's own token.

## Aspire locally

```bash
aspire new aspire-empty --language csharp -n Shop -o shop   # a file-based apphost.cs
cd shop
aspire add sqlserver          # adds Aspire.Hosting.SqlServer to apphost.cs
aspire run                    # builds, starts everything, prints the dashboard's link
aspire describe               # resources, state, health and URLs (in another terminal)
aspire wait api               # until a resource is healthy
aspire stop
```

```csharp title="apphost.cs"
var sql = builder.AddSqlServer("sql");
var db = sql.AddDatabase("Orders");               // the API gets ConnectionStrings__Orders

var api = builder.AddProject("api", "Orders.Api/Orders.Api.csproj")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject("shopfront", "Shopfront/Shopfront.csproj")
    .WithReference(api)                           // services__api__http__0 for http://api
    .WaitFor(api);
```

- The dashboard signs you in through the link `aspire run` prints: `https://localhost:17063/login?t=…`.
- ServiceDefaults' `MapDefaultEndpoints` maps `/health` and `/alive` in Development only.
- The AppHost is for your machine, not production.

## ECS on Fargate

The health and stop fields of a container definition ([parameters](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_definition_parameters.html); not run for this page):

```json
{
  "portMappings": [{ "containerPort": 8080, "protocol": "tcp" }],
  "environment": [{ "name": "Warehouse__Name", "value": "Leeds" }],
  "secrets": [
    {
      "name": "ConnectionStrings__Orders",
      "valueFrom": "arn:aws:secretsmanager:eu-west-2:123456789012:secret:orders-api/db-AbCdEf"
    }
  ],
  "stopTimeout": 45
}
```

| Field                 | Default          | Note                                                               |
| --------------------- | ---------------- | ------------------------------------------------------------------ |
| `healthCheck.command` | none             | runs inside the container: a chiseled image has no shell or `curl` |
| `stopTimeout`         | 30 s (max 120 s) | SIGKILL after this                                                 |
| `taskRoleArn`         |                  | the app's own AWS permissions                                      |
| `executionRoleArn`    |                  | pulling the image, reading `secrets`, writing logs                 |

The ALB's health check (on the target group) defaults to `/`, every 30 s, healthy after 5, unhealthy after 2. Point it at `/health/ready`.

```bash
aws ecs register-task-definition --cli-input-json file://task-definition.json
aws elbv2 create-target-group --name orders-api --protocol HTTP --port 8080 --target-type ip \
  --vpc-id vpc-0abc --health-check-path /health/ready --matcher HttpCode=200
aws ecs create-service --cluster shop --service-name orders-api --task-definition orders-api \
  --desired-count 2 --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-0abc],securityGroups=[sg-0abc]}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:eu-west-2:123456789012:targetgroup/orders-api/0abc,containerName=api,containerPort=8080"
```

## ECS, EKS or Lambda?

|                    | Suits                                                            | Trade-off                                                    |
| ------------------ | ---------------------------------------------------------------- | ------------------------------------------------------------ |
| **ECS on Fargate** | A containerized API or worker, with no cluster to run            | AWS-only; fewer knobs than Kubernetes                        |
| **EKS**            | A team already on Kubernetes, or many services sharing a cluster | $0.10 per cluster per hour, plus Kubernetes to learn and run |
| **Lambda**         | Spiky or light traffic, events, scale to zero                    | 15-minute limit, cold starts, no long-lived connections      |

```csharp title="Lambda (Amazon.Lambda.AspNetCoreServer.Hosting 2.2.1)"
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);   // or RestApi, ApplicationLoadBalancer
```

## Configuration and credentials

```csharp title="Amazon.Extensions.Configuration.SystemsManager 7.1.1"
// /orders-api/Warehouse/Name → Warehouse:Name
builder.Configuration.AddSystemsManager("/orders-api/");

// A Secrets Manager secret, through Parameter Store. A JSON secret's keys become configuration keys.
builder.Configuration.AddSystemsManager("/aws/reference/secretsmanager/orders-api/db");
```

| Runs on      | Credentials come from                                        |
| ------------ | ------------------------------------------------------------ |
| ECS          | the task role (`taskRoleArn`)                                |
| EKS          | EKS Pod Identity: a role associated with the service account |
| Lambda       | the function's execution role                                |
| Your machine | your `aws login` or SSO profile                              |

The SDK finds them by itself. AWS access keys don't go in configuration, the image, or environment variables.

More: [aspire.dev](https://aspire.dev/), [Amazon ECS](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/), [EKS Pod Identity](https://docs.aws.amazon.com/eks/latest/userguide/pod-identities.html), [ASP.NET on Lambda](https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-asp.html), and the [configuration provider](https://github.com/aws/aws-dotnet-extensions-configuration).
