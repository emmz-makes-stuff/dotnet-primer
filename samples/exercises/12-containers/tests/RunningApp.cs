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
            // Running mode fails at once if the app exits. An unhandled exception needs its own check:
            // as PID 1 the crashed process doesn't exit, so without it the test would wait out the timeout.
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(
                new UntilListening(),
                wait => wait.WithTimeout(TimeSpan.FromSeconds(60)).WithMode(WaitStrategyMode.Running)));
        foreach (var (name, value) in environment)
        {
            builder = builder.WithEnvironment(name, value);
        }

        var app = new RunningApp(builder.Build());
        try
        {
            await app._container.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            var log = await app.LogTextAsync();
            await app.DisposeAsync();
            throw new InvalidOperationException(
                $"A container of the {image.Kind} image started, but the app in it didn't: it never logged \"Now listening on\". " +
                "If you changed how the app starts, run it with `dotnet run` to see why. What the app wrote:" +
                $"{Environment.NewLine}{log}",
                e);
        }

        return app;
    }

    /// <summary>The app's stdout and stderr, or a note saying why there are none, for a failure message.</summary>
    private async Task<string> LogTextAsync()
    {
        try
        {
            var (stdout, stderr) = await _container.GetLogsAsync(timestampsEnabled: false);
            var log = (stdout + stderr).Trim();
            return log.Length > 0 ? log : "(nothing)";
        }
        catch (Exception e) when (e is DockerApiException or InvalidOperationException)
        {
            return $"(Docker couldn't return the log: {e.Message})";
        }
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
    /// <remarks>
    /// The process list shows users as the host sees them. On rootless Docker, or with userns-remap,
    /// the container's root is an ordinary user on the host, so there the container's configured user
    /// is read instead: the image's USER, or ContainerUser. Empty means root.
    /// </remarks>
    public async Task<string> ProcessUserAsync()
    {
        using var docker = DockerApi.Client();

        var engine = await docker.System.GetSystemInfoAsync(TestContext.Current.CancellationToken);
        if (MapsUsers(engine.SecurityOptions ?? []))
        {
            var container = await docker.Containers.InspectContainerAsync(_container.Id, TestContext.Current.CancellationToken);
            return ConfiguredUser(container.Config?.User);
        }

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

    /// <summary>Whether the engine maps container users to other host users: <c>name=rootless</c> or <c>name=userns</c>.</summary>
    private static bool MapsUsers(IEnumerable<string> securityOptions) =>
        securityOptions.Any(option => option.StartsWith("name=rootless", StringComparison.Ordinal)
            || option.StartsWith("name=userns", StringComparison.Ordinal));

    /// <summary>A container's configured user, such as <c>1654</c>, <c>app</c> or <c>0:0</c>, as one user name or id; empty is root.</summary>
    private static string ConfiguredUser(string? user)
    {
        var name = (user ?? "").Split(':')[0];
        return name.Length == 0 ? "root" : name;
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

    /// <summary>Ready once ASP.NET Core logs "Now listening on"; failed once .NET logs an unhandled exception.</summary>
    private sealed class UntilListening : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            var (stdout, stderr) = await container.GetLogsAsync(timestampsEnabled: false);
            if (stdout.Contains("Now listening on", StringComparison.Ordinal))
            {
                return true;
            }

            return (stdout + stderr).Contains("Unhandled exception.", StringComparison.Ordinal)
                ? throw new InvalidOperationException("The app stopped with an unhandled exception before it was listening.")
                : false;
        }
    }

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
