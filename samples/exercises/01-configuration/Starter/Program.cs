var builder = WebApplication.CreateBuilder(args);

// TODO 1: Bind the "Greeting" configuration section to GreetingOptions.
//         Hint: builder.Services.Configure<GreetingOptions>(...) with
//         builder.Configuration.GetSection(GreetingOptions.SectionName).

var app = builder.Build();

// TODO 2: Return GreetingOptions.Message from GET /greeting instead of 501.
//         Hint: ask for IOptions<GreetingOptions> as a handler parameter
//         (add `using Microsoft.Extensions.Options;` and `using Configuration;`).
app.MapGet("/greeting", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
