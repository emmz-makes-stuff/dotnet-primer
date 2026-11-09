using DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// One counter for the whole app: every request must see the same total.
builder.Services.AddSingleton<VisitCounter>();

// One RequestContext per request, shared by everything that asks for it
// during that request.
builder.Services.AddScoped<RequestContext>();

// Stateless, so a new one each time it's asked for is fine. It depends on a
// scoped service, so it must not be a singleton (that would be a captive
// dependency, and the app would fail to start in Development).
builder.Services.AddTransient<RequestContextReporter>();

var app = builder.Build();

app.MapGet("/visits", (VisitCounter counter) => counter.Next());

app.MapGet("/request-context", (RequestContext context, RequestContextReporter reporter) =>
    new RequestContextReport(context.Id, reporter.ContextId));

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
