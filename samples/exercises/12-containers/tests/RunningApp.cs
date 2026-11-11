using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace ContainerOrders.Tests;

/// <summary>
/// One container of an app image, started the way a host would: no port settings of the test's own,
/// only the environment variables a test passes.
/// </summary>
/// <remarks>
/// You don't need to change anything here to do the exercise.
/// </remarks>
public sealed partial class RunningApp : IAsyncDisposable
{
    /// <summary>The port the .NET base images tell ASP.NET Core to listen on (ASPNETCORE_HTTP_PORTS).</summary>
    public const int ImagePort = 8080;

    private readonly IContainer _container;

    private RunningApp(IContainer container) => _container = container;

    public static async Task<RunningApp> StartAsync(AppImage image, params (string Name, string Value)[] environment)
    {
        var builder = new ContainerBuilder(image.Name)
            .WithPortBinding(ImagePort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(
                "Now listening on", wait => wait.WithTimeout(TimeSpan.FromSeconds(60))));
        foreach (var (name, value) in environment)
        {
            builder = builder.WithEnvironment(name, value);
        }

        var app = new RunningApp(builder.Build());
        await app._container.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    /// <summary>What the app has written to stdout so far, without Docker's timestamps.</summary>
    public async Task<IReadOnlyList<string>> LogLinesAsync()
    {
        var (stdout, _) = await _container.GetLogsAsync(ct: TestContext.Current.CancellationToken);
        return [.. stdout.Split('\n').Select(line => DockerTimestamp().Replace(line, "").TrimEnd())];
    }

    /// <summary>The address in ASP.NET Core's "Now listening on" line, such as <c>http://[::]:8080</c>.</summary>
    public async Task<string?> ListeningOnAsync() =>
        (await LogLinesAsync()).Select(line => ListeningOn().Match(line)).FirstOrDefault(match => match.Success)?.Groups[1].Value;

    /// <summary>The value of the app's "Warehouse: …" startup line, if it logged one.</summary>
    public async Task<string?> LoggedWarehouseAsync() =>
        (await LogLinesAsync()).Select(line => LoggedWarehouse().Match(line)).FirstOrDefault(match => match.Success)?.Groups[1].Value;

    /// <summary><c>GET /info</c> on the image's port, or null if nothing answers there.</summary>
    public async Task<string?> InfoAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var info = await http.GetFromJsonAsync<Info>(
                $"http://{_container.Hostname}:{_container.GetMappedPublicPort(ImagePort)}/info",
                TestContext.Current.CancellationToken);
            return info?.Warehouse;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// The user the app's process runs as, from Docker's process list, because a chiseled image has no
    /// shell to ask. Reading /proc/1/status by copying it out doesn't work: /proc files have no size.
    /// </summary>
    public async Task<string> ProcessUserAsync()
    {
        using var docker = DockerApi.Client();
        var processes = await docker.Containers.ListProcessesAsync(
            _container.Id, new ContainerListProcessesParameters(), TestContext.Current.CancellationToken);

        // dockerd on Linux runs `ps -ef` (a UID column); some engines run BusyBox ps (a USER column).
        var column = processes.Titles.ToList().FindIndex(title => title is "UID" or "USER");
        if (column < 0 || processes.Processes.Count == 0)
        {
            throw new InvalidOperationException($"Docker's process list had no user column: [{string.Join(", ", processes.Titles)}].");
        }

        return processes.Processes[0][column];
    }

    /// <summary>Whether a file or folder exists in the container, asked of Docker rather than of a shell.</summary>
    public async Task<bool> ExistsAsync(string path)
    {
        using var docker = DockerApi.Client();
        try
        {
            await docker.Containers.GetArchiveFromContainerAsync(
                _container.Id, new ContainerPathStatParameters { Path = path }, statOnly: true, TestContext.Current.CancellationToken);
            return true;
        }
        catch (DockerContainerNotFoundException)
        {
            return false;
        }
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    private sealed record Info(string? Warehouse);

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\S+Z ")]
    private static partial Regex DockerTimestamp();

    [GeneratedRegex(@"Now listening on: (\S+)")]
    private static partial Regex ListeningOn();

    [GeneratedRegex(@"Warehouse: (.*)$")]
    private static partial Regex LoggedWarehouse();
}

/// <summary>Docker API calls that Testcontainers doesn't wrap.</summary>
internal static class DockerApi
{
    public static DockerClient Client() =>
        TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientBuilder(Guid.NewGuid()).Build();

    public static async Task RemoveImageAsync(string name)
    {
        using var docker = Client();
        try
        {
            await docker.Images.DeleteImageAsync(name, new ImageDeleteParameters { Force = true });
        }
        catch (DockerImageNotFoundException)
        {
            // Already gone: nothing to clean up.
        }
    }
}
