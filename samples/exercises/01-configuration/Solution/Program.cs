using Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Bind the "Greeting" section to GreetingOptions. Every configuration layer
// (appsettings.json, appsettings.{Environment}.json, user secrets, environment
// variables, command line) feeds this binding; later layers win.
builder.Services.Configure<GreetingOptions>(
    builder.Configuration.GetSection(GreetingOptions.SectionName));

var app = builder.Build();

app.MapGet("/greeting", (IOptions<GreetingOptions> options) => options.Value.Message);

app.Run();

/// <summary>Entry point; public so the tests' WebApplicationFactory can reach it.</summary>
public partial class Program;
