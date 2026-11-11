using System.Diagnostics;
using System.Reflection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Images;

[assembly: AssemblyFixture(typeof(ContainerOrders.Tests.SdkImage))]
[assembly: AssemblyFixture(typeof(ContainerOrders.Tests.DockerfileImage))]

namespace ContainerOrders.Tests;

/// <summary>
/// One image of the app, built once for the whole test run, because building is the slow part.
/// The tests check the image itself, by running it, not the files that build it.
/// </summary>
/// <remarks>
/// You don't need to change anything here to do the exercise.
/// </remarks>
public abstract class AppImage : IAsyncLifetime
{
    /// <summary>
    /// The app project's folder (Starter or Solution), which the test project puts in its
    /// assembly's metadata.
    /// </summary>
    public static string AppDirectory { get; } =
        typeof(AppImage).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "AppDirectory").Value
        ?? throw new InvalidOperationException("The test project doesn't say where the app is.");

    /// <summary>The app's project name, such as <c>Containers.Starter</c>: its one <c>.csproj</c>, without the extension.</summary>
    public static string ProjectName { get; } =
        Path.GetFileNameWithoutExtension(Directory.GetFiles(AppDirectory, "*.csproj").Single());

    private string? _name;

    /// <summary>The image's name and tag, once it's built.</summary>
    public string Name => _name ?? throw new InvalidOperationException($"The {Kind} image wasn't built.");

    /// <summary>How the image was built, for test messages.</summary>
    public abstract string Kind { get; }

    /// <summary>The TODO that fixes this image's user and base.</summary>
    public abstract string PackagingTodo { get; }

    /// <summary>A name that's unique to this app and this run, so runs never use each other's images.</summary>
    protected static string NewName(string suffix) =>
        $"primer-12-{ProjectName.ToLowerInvariant().Replace('.', '-')}-{suffix}:{Guid.NewGuid().ToString("N")[..12]}";

    public async ValueTask InitializeAsync()
    {
        await EnsureDockerIsRunningAsync();
        _name = await BuildAsync();
    }

    protected abstract Task<string> BuildAsync();

    public virtual async ValueTask DisposeAsync()
    {
        if (_name is not null)
        {
            await DockerApi.RemoveImageAsync(_name);
        }

        GC.SuppressFinalize(this);
    }

    private const string _dockerNeeded =
        "Exercise 12's tests need a running Docker engine, because they build the app's images and run them. " +
        "Start Docker and run the tests again. Setting it up: https://dotnet-primer.emmz.dev/platform/docker/";

    /// <summary>
    /// Checks both ways the tests reach Docker: the <c>docker</c> command, which <c>dotnet publish</c> uses,
    /// and Testcontainers, which builds and runs the containers. Either can find an engine the other
    /// can't, so a failure says Docker is missing rather than blaming a TODO.
    /// </summary>
    private static async Task EnsureDockerIsRunningAsync()
    {
        ChildProcess.Result docker;
        try
        {
            docker = await ChildProcess.RunAsync(
                "docker", Environment.CurrentDirectory, TimeSpan.FromSeconds(30), "info", "--format", "{{.ServerVersion}}");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new InvalidOperationException($"There's no `docker` command on the PATH. {_dockerNeeded}", e);
        }
        catch (TimeoutException e)
        {
            throw new InvalidOperationException(
                $"Docker didn't answer: {e.Message} It may still be starting. {_dockerNeeded}", e);
        }

        if (docker.ExitCode != 0)
        {
            throw new InvalidOperationException($"`docker info` failed: {docker.Output.Trim()}{Environment.NewLine}{_dockerNeeded}");
        }

        try
        {
            // Build() is where Testcontainers looks for Docker; nothing is started.
            _ = new ContainerBuilder("mcr.microsoft.com/dotnet/aspnet:10.0").Build();
        }
        catch (DockerUnavailableException e)
        {
            throw new InvalidOperationException(_dockerNeeded, e);
        }
    }
}

/// <summary>Runs a command to completion, and keeps everything it printed.</summary>
internal static class ChildProcess
{
    public sealed record Result(int ExitCode, string Output);

    /// <summary>
    /// Runs the command, and kills it if it takes longer than <paramref name="timeout"/>, so a hung
    /// command fails the run with its name rather than hanging it.
    /// </summary>
    /// <exception cref="TimeoutException">The command didn't finish in time.</exception>
    public static async Task<Result> RunAsync(string fileName, string workingDirectory, TimeSpan timeout, params string[] arguments)
    {
        var start = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // The test host runs under `dotnet test`, whose MSBuild settings would leak into a nested build.
        foreach (var variable in new[] { "MSBuildExtensionsPath", "MSBuildSDKsPath", "MSBUILD_EXE_PATH", "MSBuildLoadMicrosoftTargetsReadOnly" })
        {
            start.Environment.Remove(variable);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Couldn't start `{fileName}`.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();

        using var timer = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timer.Token);
        }
        catch (OperationCanceledException) when (timer.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"`{fileName} {string.Join(' ', arguments)}` didn't finish within {timeout.TotalSeconds:0} s, so it was stopped.");
        }

        return new Result(process.ExitCode, await output + await errors);
    }
}

/// <summary>The image that <c>dotnet publish -t:PublishContainer</c> builds, from the project's container properties.</summary>
public sealed class SdkImage : AppImage
{
    public override string Kind => "SDK-published";

    public override string PackagingTodo => "TODO 3";

    protected override async Task<string> BuildAsync()
    {
        var name = NewName("sdk");
        var (repository, tag) = (name[..name.IndexOf(':', StringComparison.Ordinal)], name[(name.IndexOf(':', StringComparison.Ordinal) + 1)..]);

        // Ten minutes allows for a first run that downloads the base image on a slow connection.
        var publish = await ChildProcess.RunAsync("dotnet", AppDirectory, TimeSpan.FromMinutes(10),
            "publish", "-t:PublishContainer", "-c", "Release",
            $"-p:ContainerRepository={repository}", $"-p:ContainerImageTag={tag}");

        if (publish.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"`dotnet publish -t:PublishContainer` failed for {ProjectName} (exit code {publish.ExitCode}), so there's no " +
                $"image to test. Check the .csproj's container properties (TODO 3). Its output:{Environment.NewLine}" +
                publish.Output);
        }

        return name;
    }
}

/// <summary>The image that the app's own <c>Dockerfile</c> builds, with the app's folder as the build context.</summary>
public sealed class DockerfileImage : AppImage
{
    public override string Kind => "Dockerfile";

    public override string PackagingTodo => "TODO 4";

    protected override async Task<string> BuildAsync()
    {
        // The base class removes the image after the run. WithCleanUp has Testcontainers remove it too,
        // if the test process dies first.
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(AppDirectory)
            .WithDockerfile("Dockerfile")
            .WithName(NewName("dockerfile"))
            .WithCleanUp(true)
            .Build();

        try
        {
            await image.CreateAsync();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"`docker build` failed for {ProjectName}'s Dockerfile, so there's no image to test. Check the " +
                $"Dockerfile (TODO 4). What Docker said:{Environment.NewLine}{e.Message}",
                e);
        }

        return image.FullName;
    }
}
