---
title: "Coming from .NET 3.5"
description: "Old habits, new equivalents: a map from an ASP.NET MVC, Web API and WCF codebase on .NET Framework 3.5 to modern .NET, each row linked to the page that teaches it."
prev: false
next: false
learn:
  - title: "Port from .NET Framework to .NET"
    url: https://learn.microsoft.com/dotnet/core/porting/
  - title: "Migrate from ASP.NET to ASP.NET Core"
    url: https://learn.microsoft.com/aspnet/core/migration/fx-to-core/
---

Start from something you know in the old codebase, find it below, and follow the link to the page that explains what replaced it. Each page has its own "Coming from" section with more detail.

:::tip[Cheatsheet]
The [Coming from .NET 3.5 cheatsheet](/cheatsheets/coming-from-3-5/) has the same map in two columns, with each modern equivalent linked to the page that teaches it.
:::

This is a map of ideas, not a porting guide: it says what each thing became, not how to move a project file. For that, see the Learn guides at the end of the page.

## `web.config` and hosting

The app stops being something IIS loads, and becomes a program that hosts itself. Configuration moves out of one XML file into layers of key–value sources.

| .NET 3.5                                                | Modern                                                          | Read                                                            |
| ------------------------------------------------------- | --------------------------------------------------------------- | --------------------------------------------------------------- |
| `Global.asax` and `Application_Start`                   | `Program.cs`: the builder, `Build`, then `Run`                  | [ASP.NET Core](/aspnet-core/#coming-from-net-35)                |
| An app that IIS loads into its own pipeline             | A console program that runs Kestrel, its own web server         | [ASP.NET Core](/aspnet-core/#coming-from-net-35)                |
| `<appSettings>` and `ConfigurationManager.AppSettings`  | `IConfiguration`, from `appsettings.json` and other sources     | [Configuration](/aspnet-core/configuration/#coming-from-net-35) |
| `<connectionStrings>`                                   | A `ConnectionStrings` section, and `GetConnectionString`        | [Configuration](/aspnet-core/configuration/#coming-from-net-35) |
| `Web.Release.config` transforms                         | `appsettings.Production.json`, and environment variables        | [Configuration](/aspnet-core/configuration/#coming-from-net-35) |
| A custom `ConfigurationSection`                         | The options pattern: a plain class bound to a section           | [Configuration](/aspnet-core/configuration/#coming-from-net-35) |
| Secrets in `web.config`, encrypted with `aspnet_regiis` | User secrets locally, and a secret store in production          | [Security & auth](/security/#coming-from-net-35)                |
| `machine.config`, shared by every app on the server     | Nothing shared: each image carries its own runtime and settings | [Containers](/deploy/containers/#coming-from-net-35)            |

## MVC and Web API

MVC and Web API were two stacks side by side, each with its own controllers, routing, filters and dependency resolver. ASP.NET Core has one of each, and minimal APIs besides.

| .NET 3.5                                                  | Modern                                                            | Read                                                                          |
| --------------------------------------------------------- | ----------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| `System.Web.Mvc.Controller` and Web API's `ApiController` | One controller stack, `ControllerBase`, or minimal APIs           | [ASP.NET Core](/aspnet-core/#coming-from-net-35)                              |
| `RouteConfig` and `WebApiConfig`                          | `app.MapGet` and the other `Map…` methods, or `MapControllers`    | [ASP.NET Core](/aspnet-core/#coming-from-net-35)                              |
| `IHttpActionResult`, `Request.CreateResponse`             | Typed results, such as `TypedResults.NotFound()`                  | [ASP.NET Core](/aspnet-core/#coming-from-net-35)                              |
| MVC's `DependencyResolver` and Web API's, side by side    | One built-in container for everything                             | [Dependency Injection](/aspnet-core/dependency-injection/#coming-from-net-35) |
| Unity, Autofac or Ninject wiring                          | `AddSingleton`, `AddScoped` and `AddTransient`                    | [Dependency Injection](/aspnet-core/dependency-injection/#coming-from-net-35) |
| Static singletons, and `new` where they're used           | Constructor injection                                             | [Dependency Injection](/aspnet-core/dependency-injection/#coming-from-net-35) |
| `IHttpModule`, and `Application_BeginRequest`             | Middleware                                                        | [Middleware](/aspnet-core/middleware/#coming-from-net-35)                     |
| Web API's `DelegatingHandler`                             | Middleware, in front of every endpoint                            | [Middleware](/aspnet-core/middleware/#coming-from-net-35)                     |
| MVC's filters and Web API's filters                       | One set of filters for controllers, and endpoint filters          | [Middleware](/aspnet-core/middleware/#coming-from-net-35)                     |
| `Application_Error` and `<customErrors>`                  | `UseExceptionHandler` and Problem Details                         | [Middleware](/aspnet-core/middleware/#coming-from-net-35)                     |
| `HttpContext.Current.User`                                | `HttpContext.User`, or the `ClaimsPrincipal` an endpoint is given | [Security & auth](/security/#coming-from-net-35)                              |

## WCF

gRPC replaces WCF for service-to-service calls: the contract moves from attributes on C# into a `.proto` file, and the transport is HTTP/2 everywhere.

| .NET 3.5                                      | Modern                                                | Read                                    |
| --------------------------------------------- | ----------------------------------------------------- | --------------------------------------- |
| `[ServiceContract]` and `[OperationContract]` | A `service` and its `rpc`s in a `.proto`              | [gRPC](/services/grpc/#coming-from-wcf) |
| `[DataContract]` and `[DataMember]`           | `message`s, with field numbers on the wire            | [gRPC](/services/grpc/#coming-from-wcf) |
| `netTcpBinding`                               | gRPC over HTTP/2, binary and cross-platform           | [gRPC](/services/grpc/#coming-from-wcf) |
| Bindings in `<system.serviceModel>`           | HTTP/2 on Kestrel, configured in code                 | [gRPC](/services/grpc/#coming-from-wcf) |
| `svcutil` and generated proxies               | A client generated from the `.proto` on every build   | [gRPC](/services/grpc/#coming-from-wcf) |
| `FaultException<T>`                           | An `RpcException` with a status code and rich details | [gRPC](/services/grpc/#coming-from-wcf) |
| Duplex callback contracts                     | Bidirectional streaming                               | [gRPC](/services/grpc/#coming-from-wcf) |

To move WCF services as they are, there's [CoreWCF](https://github.com/CoreWCF/CoreWCF); the gRPC page says when that makes sense.

## Session and state

Session still exists, but it's built on a distributed cache, has no lock, and needs shared keys to work on more than one server.

| .NET 3.5                                   | Modern                                                | Read                                                                          |
| ------------------------------------------ | ----------------------------------------------------- | ----------------------------------------------------------------------------- |
| `Session["basket"] = basket`               | `ISession`, with objects stored as JSON               | [Session and state](/aspnet-core/session-and-state/#coming-from-net-35)       |
| `mode="InProc"`                            | `AddDistributedMemoryCache`, for one server only      | [Session and state](/aspnet-core/session-and-state/#coming-from-net-35)       |
| `mode="StateServer"` or `mode="SQLServer"` | A shared `IDistributedCache`, such as Redis           | [Session and state](/aspnet-core/session-and-state/#coming-from-net-35)       |
| The session lock                           | No lock: concurrent requests can overwrite each other | [Session and state](/aspnet-core/session-and-state/#coming-from-net-35)       |
| `Session_End`                              | No event                                              | [Session and state](/aspnet-core/session-and-state/#coming-from-net-35)       |
| `<machineKey>` on every server in the farm | A shared Data Protection key ring                     | [Security & auth](/security/#coming-from-net-35)                              |
| `HttpContext.Current.Items`, per request   | A scoped service                                      | [Dependency Injection](/aspnet-core/dependency-injection/#coming-from-net-35) |
| `HttpRuntime.Cache`                        | `HybridCache`, or output caching                      | [Async & performance](/performance/async-and-performance/#coming-from-net-35) |

## Data

EF Core is a rewrite of Entity Framework: the ideas carry over, but the model is code only, and nothing loads lazily unless you ask.

| .NET 3.5                                       | Modern                                                   | Read                                                     |
| ---------------------------------------------- | -------------------------------------------------------- | -------------------------------------------------------- |
| An EDMX model and the designer                 | Code first only, kept in step by migrations              | [EF Core](/data/ef-core/#coming-from-entity-framework-6) |
| Lazy loading on by default                     | Off by default: `Include`, or project with `Select`      | [EF Core](/data/ef-core/#coming-from-entity-framework-6) |
| `ObjectContext`, or a `DbContext` you `new` up | A `DbContext` from dependency injection, one per request | [EF Core](/data/ef-core/#coming-from-entity-framework-6) |
| Migrations with the model in a resource        | Migrations with a C# snapshot, run with `dotnet ef`      | [EF Core](/data/ef-core/#coming-from-entity-framework-6) |
| `System.Data.Entity`                           | `Microsoft.EntityFrameworkCore`, a package per database  | [EF Core](/data/ef-core/#coming-from-entity-framework-6) |

## Diagnostics

Logs, traces and metrics come from one set of APIs built into .NET, exported with OpenTelemetry.

| .NET 3.5                                         | Modern                                     | Read                                                  |
| ------------------------------------------------ | ------------------------------------------ | ----------------------------------------------------- |
| log4net or Enterprise Library                    | `ILogger<T>`                               | [Logging](/observability/logging/#coming-from-net-35) |
| `Trace.WriteLine` and trace listeners            | `ILogger`, to the console or as JSON       | [Logging](/observability/logging/#coming-from-net-35) |
| `log.Info("Order " + id + " placed")`            | A message template with named placeholders | [Logging](/observability/logging/#coming-from-net-35) |
| A correlation id passed by hand                  | Trace context, carried for you             | [Tracing](/observability/tracing/#coming-from-net-35) |
| WCF message logging, `System.Diagnostics` traces | Spans from an `ActivitySource`             | [Tracing](/observability/tracing/#coming-from-net-35) |
| Windows performance counters                     | A `Meter` and its instruments              | [Metrics](/observability/metrics/#coming-from-net-35) |
| `perfmon`                                        | `dotnet-counters`, or the dashboard        | [Metrics](/observability/metrics/#coming-from-net-35) |

## Threading and async

The older asynchronous patterns all become `async` and `await`, and long-running work gets a host-managed home.

| .NET 3.5                                     | Modern                                    | Read                                                                          |
| -------------------------------------------- | ----------------------------------------- | ----------------------------------------------------------------------------- |
| `BeginXxx`/`EndXxx` and `IAsyncResult`       | `async` and `await`                       | [Async & performance](/performance/async-and-performance/#coming-from-net-35) |
| `BackgroundWorker` and `XxxCompleted` events | `await`, with `IProgress<T>` for progress | [Async & performance](/performance/async-and-performance/#coming-from-net-35) |
| `ThreadPool.QueueUserWorkItem`               | `Task.Run`, for CPU-bound work only       | [Async & performance](/performance/async-and-performance/#coming-from-net-35) |
| `new Thread(…)` for background work          | A `BackgroundService`                     | [Async & performance](/performance/async-and-performance/#coming-from-net-35) |
| `Thread.Sleep`                               | `await Task.Delay(…, cancellationToken)`  | [Async & performance](/performance/async-and-performance/#coming-from-net-35) |

## Testing

| .NET 3.5                              | Modern                                          | Read                                    |
| ------------------------------------- | ----------------------------------------------- | --------------------------------------- |
| `[TestClass]`, `[TestMethod]`         | A plain class, and `[Fact]`                     | [Testing](/testing/#coming-from-net-35) |
| `[TestCase]` or `[DataSource]`        | `[Theory]` with `[InlineData]`                  | [Testing](/testing/#coming-from-net-35) |
| `[ExpectedException]`                 | `Assert.Throws<T>` around the one call          | [Testing](/testing/#coming-from-net-35) |
| `[SetUp]` and `[TearDown]`            | The constructor, and `Dispose`                  | [Testing](/testing/#coming-from-net-35) |
| `DateTime.Now` in the code under test | `TimeProvider`, and `FakeTimeProvider` in tests | [Testing](/testing/#coming-from-net-35) |
| Rhino Mocks or TypeMock               | A hand-written fake, or NSubstitute             | [Testing](/testing/#coming-from-net-35) |
| `mstest.exe` and `vstest.console`     | `dotnet test`                                   | [Testing](/testing/#coming-from-net-35) |

## Security

| .NET 3.5                               | Modern                                                   | Read                                             |
| -------------------------------------- | -------------------------------------------------------- | ------------------------------------------------ |
| Forms authentication                   | Cookie authentication, with OpenID Connect               | [Security & auth](/security/#coming-from-net-35) |
| Windows authentication in IIS          | The Negotiate handler, or a federating identity provider | [Security & auth](/security/#coming-from-net-35) |
| `<authorization>` in `web.config`      | `[Authorize]`, policies and the fallback policy, in code | [Security & auth](/security/#coming-from-net-35) |
| Membership and role providers          | An identity provider, or ASP.NET Core Identity           | [Security & auth](/security/#coming-from-net-35) |
| Request validation (`ValidateRequest`) | No equivalent: encode output, and validate input         | [Security & auth](/security/#coming-from-net-35) |

## Deployment

The server stops being something you look after, and becomes something you replace: an image, run by an orchestrator that checks its health.

| .NET 3.5                                    | Modern                                                     | Read                                                                     |
| ------------------------------------------- | ---------------------------------------------------------- | ------------------------------------------------------------------------ |
| Copying files to IIS, Web Deploy, or an MSI | An image, built once and run anywhere                      | [Containers](/deploy/containers/#coming-from-net-35)                     |
| An app pool running as `LocalSystem`        | The non-root `app` user                                    | [Containers](/deploy/containers/#coming-from-net-35)                     |
| Windows Update on the server                | Rebuilding the image on an updated base                    | [Containers](/deploy/containers/#coming-from-net-35)                     |
| Remote Desktop onto the server              | `docker logs`, `docker top` and `docker cp`                | [Containers](/deploy/containers/#coming-from-net-35)                     |
| The load balancer pinging `ping.aspx`       | Readiness and liveness endpoints                           | [Orchestration & AWS](/deploy/orchestration-and-aws/#coming-from-net-35) |
| IIS recycling the app pool                  | The orchestrator replacing a container that fails liveness | [Orchestration & AWS](/deploy/orchestration-and-aws/#coming-from-net-35) |
| `app_offline.htm` during a deployment       | A rolling deployment, with draining                        | [Orchestration & AWS](/deploy/orchestration-and-aws/#coming-from-net-35) |
| Changing settings on the server             | Parameter Store, Secrets Manager, then a new deployment    | [Orchestration & AWS](/deploy/orchestration-and-aws/#coming-from-net-35) |
