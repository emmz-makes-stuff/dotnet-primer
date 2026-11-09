using Middleware;

var builder = WebApplication.CreateBuilder(args);

// Unhandled exceptions become RFC 9457 Problem Details responses.
builder.Services.AddProblemDetails();

var app = builder.Build();

// TODO 4: Register CorrelationIdMiddleware with app.UseMiddleware<CorrelationIdMiddleware>().
//         Where it goes matters: middleware runs in the order it's registered,
//         and MaintenanceModeMiddleware short-circuits (it answers 503 without
//         calling the rest of the pipeline). Every response, including that 503,
//         needs a correlation id.
app.UseExceptionHandler();
app.UseMiddleware<MaintenanceModeMiddleware>();

app.MapGet("/hello", () => "Hello!");
app.MapGet("/boom", string () => throw new InvalidOperationException("Boom!"));

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
