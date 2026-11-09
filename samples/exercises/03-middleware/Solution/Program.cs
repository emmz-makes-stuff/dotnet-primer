using Middleware;

var builder = WebApplication.CreateBuilder(args);

// Unhandled exceptions become RFC 9457 Problem Details responses.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Order matters: each middleware only sees requests that the ones before it
// pass on. The exception handler goes first, so it can catch exceptions from
// everything after it, this middleware included. The correlation id must be
// set up before anything that can short-circuit, such as maintenance mode, or
// those responses won't have it.
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<MaintenanceModeMiddleware>();

app.MapGet("/hello", () => "Hello!");
app.MapGet("/boom", string () => throw new InvalidOperationException("Boom!"));

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
