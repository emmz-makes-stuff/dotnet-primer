---
title: Coming from .NET 3.5 cheatsheet
description: Old habits and their modern equivalents, from web.config and hosting to deployment, in two columns on one page.
prev: false
next: false
learn:
  - title: Port from .NET Framework to .NET
    url: https://learn.microsoft.com/dotnet/core/porting/
  - title: Migrate from ASP.NET to ASP.NET Core
    url: https://learn.microsoft.com/aspnet/core/migration/fx-to-core/
---

Back to [Coming from .NET 3.5](/coming-from-3-5/).

## `web.config` and hosting

| .NET 3.5                                                | Modern                                                                                                        |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `Global.asax` and `Application_Start`                   | [`Program.cs`: the builder, `Build`, then `Run`](/aspnet-core/#coming-from-net-35)                            |
| An app that IIS loads into its own pipeline             | [A console program that runs Kestrel, its own web server](/aspnet-core/#coming-from-net-35)                   |
| `<appSettings>` and `ConfigurationManager.AppSettings`  | [`IConfiguration`, from `appsettings.json` and other sources](/aspnet-core/configuration/#coming-from-net-35) |
| `<connectionStrings>`                                   | [A `ConnectionStrings` section, and `GetConnectionString`](/aspnet-core/configuration/#coming-from-net-35)    |
| `Web.Release.config` transforms                         | [`appsettings.Production.json`, and environment variables](/aspnet-core/configuration/#coming-from-net-35)    |
| A custom `ConfigurationSection`                         | [The options pattern: a plain class bound to a section](/aspnet-core/configuration/#coming-from-net-35)       |
| Secrets in `web.config`, encrypted with `aspnet_regiis` | [User secrets locally, and a secret store in production](/security/#coming-from-net-35)                       |
| `machine.config`, shared by every app on the server     | [Nothing shared: each image carries its own runtime and settings](/deploy/containers/#coming-from-net-35)     |

## MVC and Web API

| .NET 3.5                                                  | Modern                                                                                                  |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `System.Web.Mvc.Controller` and Web API's `ApiController` | [One controller stack, `ControllerBase`, or minimal APIs](/aspnet-core/#coming-from-net-35)             |
| `RouteConfig` and `WebApiConfig`                          | [`app.MapGet` and the other `Map…` methods, or `MapControllers`](/aspnet-core/#coming-from-net-35)      |
| `IHttpActionResult`, `Request.CreateResponse`             | [Typed results, such as `TypedResults.NotFound()`](/aspnet-core/#coming-from-net-35)                    |
| MVC's `DependencyResolver` and Web API's, side by side    | [One built-in container for everything](/aspnet-core/dependency-injection/#coming-from-net-35)          |
| Unity, Autofac or Ninject wiring                          | [`AddSingleton`, `AddScoped` and `AddTransient`](/aspnet-core/dependency-injection/#coming-from-net-35) |
| Static singletons, and `new` where they're used           | [Constructor injection](/aspnet-core/dependency-injection/#coming-from-net-35)                          |
| `IHttpModule`, and `Application_BeginRequest`             | [Middleware](/aspnet-core/middleware/#coming-from-net-35)                                               |
| Web API's `DelegatingHandler`                             | [Middleware, in front of every endpoint](/aspnet-core/middleware/#coming-from-net-35)                   |
| MVC's filters and Web API's filters                       | [One set of filters for controllers, and endpoint filters](/aspnet-core/middleware/#coming-from-net-35) |
| `Application_Error` and `<customErrors>`                  | [`UseExceptionHandler` and Problem Details](/aspnet-core/middleware/#coming-from-net-35)                |
| `HttpContext.Current.User`                                | [`HttpContext.User`, or the `ClaimsPrincipal` an endpoint is given](/security/#coming-from-net-35)      |

## WCF

| .NET 3.5                                      | Modern                                                                                   |
| --------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `[ServiceContract]` and `[OperationContract]` | [A `service` and its `rpc`s in a `.proto`](/services/grpc/#coming-from-wcf)              |
| `[DataContract]` and `[DataMember]`           | [`message`s, with field numbers on the wire](/services/grpc/#coming-from-wcf)            |
| `netTcpBinding`                               | [gRPC over HTTP/2, binary and cross-platform](/services/grpc/#coming-from-wcf)           |
| Bindings in `<system.serviceModel>`           | [HTTP/2 on Kestrel, configured in code](/services/grpc/#coming-from-wcf)                 |
| `svcutil` and generated proxies               | [A client generated from the `.proto` on every build](/services/grpc/#coming-from-wcf)   |
| `FaultException<T>`                           | [An `RpcException` with a status code and rich details](/services/grpc/#coming-from-wcf) |
| Duplex callback contracts                     | [Bidirectional streaming](/services/grpc/#coming-from-wcf)                               |

## Session and state

| .NET 3.5                                   | Modern                                                                                                      |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| `Session["basket"] = basket`               | [`ISession`, with objects stored as JSON](/aspnet-core/session-and-state/#coming-from-net-35)               |
| `mode="InProc"`                            | [`AddDistributedMemoryCache`, for one server only](/aspnet-core/session-and-state/#coming-from-net-35)      |
| `mode="StateServer"` or `mode="SQLServer"` | [A shared `IDistributedCache`, such as Redis](/aspnet-core/session-and-state/#coming-from-net-35)           |
| The session lock                           | [No lock: concurrent requests can overwrite each other](/aspnet-core/session-and-state/#coming-from-net-35) |
| `Session_End`                              | [No event](/aspnet-core/session-and-state/#coming-from-net-35)                                              |
| `<machineKey>` on every server in the farm | [A shared Data Protection key ring](/security/#coming-from-net-35)                                          |
| `HttpContext.Current.Items`, per request   | [A scoped service](/aspnet-core/dependency-injection/#coming-from-net-35)                                   |
| `HttpRuntime.Cache`                        | [`HybridCache`, or output caching](/performance/async-and-performance/#coming-from-net-35)                  |

## Data

| .NET 3.5                                       | Modern                                                                                                    |
| ---------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| An EDMX model and the designer                 | [Code first only, kept in step by migrations](/data/ef-core/#coming-from-entity-framework-6)              |
| Lazy loading on by default                     | [Off by default: `Include`, or project with `Select`](/data/ef-core/#coming-from-entity-framework-6)      |
| `ObjectContext`, or a `DbContext` you `new` up | [A `DbContext` from dependency injection, one per request](/data/ef-core/#coming-from-entity-framework-6) |
| Migrations with the model in a resource        | [Migrations with a C# snapshot, run with `dotnet ef`](/data/ef-core/#coming-from-entity-framework-6)      |
| `System.Data.Entity`                           | [`Microsoft.EntityFrameworkCore`, a package per database](/data/ef-core/#coming-from-entity-framework-6)  |

## Diagnostics

| .NET 3.5                                         | Modern                                                                                   |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| log4net or Enterprise Library                    | [`ILogger<T>`](/observability/logging/#coming-from-net-35)                               |
| `Trace.WriteLine` and trace listeners            | [`ILogger`, to the console or as JSON](/observability/logging/#coming-from-net-35)       |
| `log.Info("Order " + id + " placed")`            | [A message template with named placeholders](/observability/logging/#coming-from-net-35) |
| A correlation id passed by hand                  | [Trace context, carried for you](/observability/tracing/#coming-from-net-35)             |
| WCF message logging, `System.Diagnostics` traces | [Spans from an `ActivitySource`](/observability/tracing/#coming-from-net-35)             |
| Windows performance counters                     | [A `Meter` and its instruments](/observability/metrics/#coming-from-net-35)              |
| `perfmon`                                        | [`dotnet-counters`, or the dashboard](/observability/metrics/#coming-from-net-35)        |

## Threading and async

| .NET 3.5                                     | Modern                                                                                              |
| -------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `BeginXxx`/`EndXxx` and `IAsyncResult`       | [`async` and `await`](/performance/async-and-performance/#coming-from-net-35)                       |
| `BackgroundWorker` and `XxxCompleted` events | [`await`, with `IProgress<T>` for progress](/performance/async-and-performance/#coming-from-net-35) |
| `ThreadPool.QueueUserWorkItem`               | [`Task.Run`, for CPU-bound work only](/performance/async-and-performance/#coming-from-net-35)       |
| `new Thread(…)` for background work          | [A `BackgroundService`](/performance/async-and-performance/#coming-from-net-35)                     |
| `Thread.Sleep`                               | [`await Task.Delay(…, cancellationToken)`](/performance/async-and-performance/#coming-from-net-35)  |

## Testing

| .NET 3.5                              | Modern                                                                          |
| ------------------------------------- | ------------------------------------------------------------------------------- |
| `[TestClass]`, `[TestMethod]`         | [A plain class, and `[Fact]`](/testing/#coming-from-net-35)                     |
| `[TestCase]` or `[DataSource]`        | [`[Theory]` with `[InlineData]`](/testing/#coming-from-net-35)                  |
| `[ExpectedException]`                 | [`Assert.Throws<T>` around the one call](/testing/#coming-from-net-35)          |
| `[SetUp]` and `[TearDown]`            | [The constructor, and `Dispose`](/testing/#coming-from-net-35)                  |
| `DateTime.Now` in the code under test | [`TimeProvider`, and `FakeTimeProvider` in tests](/testing/#coming-from-net-35) |
| Rhino Mocks or TypeMock               | [A hand-written fake, or NSubstitute](/testing/#coming-from-net-35)             |
| `mstest.exe` and `vstest.console`     | [`dotnet test`](/testing/#coming-from-net-35)                                   |

## Security

| .NET 3.5                               | Modern                                                                                    |
| -------------------------------------- | ----------------------------------------------------------------------------------------- |
| Forms authentication                   | [Cookie authentication, with OpenID Connect](/security/#coming-from-net-35)               |
| Windows authentication in IIS          | [The Negotiate handler, or a federating identity provider](/security/#coming-from-net-35) |
| `<authorization>` in `web.config`      | [`[Authorize]`, policies and the fallback policy, in code](/security/#coming-from-net-35) |
| Membership and role providers          | [An identity provider, or ASP.NET Core Identity](/security/#coming-from-net-35)           |
| Request validation (`ValidateRequest`) | [No equivalent: encode output, and validate input](/security/#coming-from-net-35)         |

## Deployment

| .NET 3.5                                    | Modern                                                                                                          |
| ------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| Copying files to IIS, Web Deploy, or an MSI | [An image, built once and run anywhere](/deploy/containers/#coming-from-net-35)                                 |
| An app pool running as `LocalSystem`        | [The non-root `app` user](/deploy/containers/#coming-from-net-35)                                               |
| Windows Update on the server                | [Rebuilding the image on an updated base](/deploy/containers/#coming-from-net-35)                               |
| Remote Desktop onto the server              | [`docker logs`, `docker top` and `docker cp`](/deploy/containers/#coming-from-net-35)                           |
| The load balancer pinging `ping.aspx`       | [Readiness and liveness endpoints](/deploy/orchestration-and-aws/#coming-from-net-35)                           |
| IIS recycling the app pool                  | [The orchestrator replacing a container that fails liveness](/deploy/orchestration-and-aws/#coming-from-net-35) |
| `app_offline.htm` during a deployment       | [A rolling deployment, with draining](/deploy/orchestration-and-aws/#coming-from-net-35)                        |
| Changing settings on the server             | [Parameter Store, Secrets Manager, then a new deployment](/deploy/orchestration-and-aws/#coming-from-net-35)    |
