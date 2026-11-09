var builder = WebApplication.CreateBuilder(args);

// The services are already written: VisitCounter, RequestContext and
// RequestContextReporter. Register each one with the lifetime it needs.
// Read the comment on each class to decide.
//
// TODO 1: Register VisitCounter.
// TODO 2: Register RequestContext.
// TODO 3: Register RequestContextReporter.
//         Hint: builder.Services.AddSingleton<T>(), AddScoped<T>() or AddTransient<T>().

var app = builder.Build();

// Do TODOs 1-3 first. Until a type is registered, ASP.NET Core treats a
// handler parameter of that type as the request body, which a GET can't have,
// so every endpoint, not just that one, returns 500 ("Body was inferred but
// the method does not allow inferred body parameters").
//
// TODO 4: Return the next visit count from GET /visits instead of 501.
//         Hint: add a VisitCounter parameter to the handler and call Next().
//         (Add `using DependencyInjection;` at the top of this file.)
app.MapGet("/visits", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

// TODO 5: Return a RequestContextReport from GET /request-context instead of 501.
//         Hint: take a RequestContext and a RequestContextReporter as parameters, and
//         return new RequestContextReport(context.Id, reporter.ContextId).
app.MapGet("/request-context", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
