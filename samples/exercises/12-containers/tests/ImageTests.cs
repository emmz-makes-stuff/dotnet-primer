namespace ContainerOrders.Tests;

/// <summary>
/// The checks every image of the app must pass. The app's own code (TODOs 1 and 2) shows in both
/// images; the user and the base (TODO 3 for the SDK image, TODO 4 for the Dockerfile) in one each.
/// </summary>
public abstract class ImageTests(AppImage image)
{
    [Fact]
    public async Task AnswersOnTheImagesPort()
    {
        await using var app = await RunningApp.StartAsync(image);

        var listeningOn = await app.ListeningOnAsync();
        Assert.True(listeningOn?.EndsWith($":{RunningApp.ImagePort}", StringComparison.Ordinal) == true,
            $"In the {image.Kind} image, the app listens on {listeningOn ?? "an address it never logged"}, not on port " +
            $"{RunningApp.ImagePort}, which the .NET images set with ASPNETCORE_HTTP_PORTS. Something in the code is " +
            "choosing the port instead (TODO 1).");

        Assert.True(await app.InfoAsync() is not null,
            $"In the {image.Kind} image, the app says it listens on port {RunningApp.ImagePort}, but GET /info didn't answer there.");
    }

    [Fact]
    public async Task EnvironmentVariableOverridesTheWarehouseName()
    {
        var name = $"Warehouse from the environment {Guid.NewGuid():N}";
        await using var app = await RunningApp.StartAsync(image, ("Warehouse__Name", name));

        var logged = await app.LoggedWarehouseAsync();
        Assert.True(logged is not null,
            $"In the {image.Kind} image, the app didn't log its \"Warehouse: …\" line at startup, so the test can't tell which " +
            "warehouse name it used.");
        Assert.True(logged == name,
            $"In the {image.Kind} image, the app used the warehouse name \"{logged}\", although Warehouse__Name was set to " +
            $"\"{name}\". The setting is read in a way that environment variables can't override (TODO 2).");
    }

    [Fact]
    public async Task RunsAsANonRootUser()
    {
        await using var app = await RunningApp.StartAsync(image);

        var user = await app.ProcessUserAsync();
        Assert.False(user is "root" or "0",
            $"The {image.Kind} image runs the app as root. If anything in the app is exploited, the attacker has root in the " +
            $"container. Run it as the image's non-root `app` user ({image.PackagingTodo}).");
    }

    [Fact]
    public async Task HasNoShell()
    {
        await using var app = await RunningApp.StartAsync(image);

        Assert.False(await app.ExistsAsync("/bin/sh"),
            $"The {image.Kind} image has a shell (/bin/sh), so it isn't on a chiseled base. A chiseled base has no shell and " +
            $"no package manager, which leaves far fewer packages to patch and far less for an attacker to use " +
            $"({image.PackagingTodo}).");
    }
}

public sealed class SdkImageTests(SdkImage image) : ImageTests(image);

public sealed class DockerfileImageTests(DockerfileImage image) : ImageTests(image)
{
    [Fact]
    public async Task HasNoSdk()
    {
        await using var app = await RunningApp.StartAsync(image);

        Assert.False(await app.ExistsAsync("/usr/share/dotnet/sdk"),
            "The Dockerfile image still holds the .NET SDK, so the image you run is the image you built with: nearly 1 GB, " +
            "and a compiler for an attacker. Build in an SDK stage, and copy only the published app into a runtime image " +
            "(TODO 4).");
    }
}
