using System.Diagnostics;
using System.Text.Json;

namespace TestingOrders.Checker;

/// <summary>How a run of the tests ended, from Microsoft.Testing.Platform's exit code.</summary>
internal enum RunResult
{
    /// <summary>Exit code 0: every test passed.</summary>
    Passed,

    /// <summary>Exit code 2: at least one test failed.</summary>
    TestsFailed,

    /// <summary>Exit code 8: the project has no tests to run.</summary>
    NoTests,

    /// <summary>Any other exit code: the build or the run itself failed.</summary>
    Error,
}

/// <summary>The outcome of one <c>dotnet test</c> run, with what it printed.</summary>
internal sealed record TestRunOutcome(RunResult Result, int ExitCode, IReadOnlyList<string> FailedTests, string Output);

/// <summary>Runs a test project with <c>dotnet test</c>, against the correct API or a seeded bug.</summary>
internal static class TestRun
{
    public static async Task<TestRunOutcome> RunAsync(string testProject, string exerciseRoot, string runDirectory, SeededBug? bug)
    {
        var resultsDirectory = Path.Combine(runDirectory, "results");
        const string ReportName = "results.ctrf";
        if (Directory.Exists(resultsDirectory))
        {
            Directory.Delete(resultsDirectory, recursive: true);
        }

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = exerciseRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        string[] arguments =
        [
            "test", "--project", testProject,
            // Each run builds into its own folder, so the bug builds never share obj/ with the
            // correct one, and a later normal build is unaffected.
            "--artifacts-path", Path.Combine(runDirectory, "artifacts"),
            "--report-xunit-ctrf", "--report-xunit-ctrf-filename", ReportName,
            "--results-directory", resultsDirectory,
        ];
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        if (bug is not null)
        {
            start.ArgumentList.Add($"-p:SeededBug={bug.Number}");
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Couldn't start dotnet.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await stdout + await stderr;

        var result = process.ExitCode switch
        {
            0 => RunResult.Passed,
            2 => RunResult.TestsFailed,
            8 => RunResult.NoTests,
            _ => RunResult.Error,
        };
        var failed = result == RunResult.TestsFailed ? FailedTests(Path.Combine(resultsDirectory, ReportName)) : [];
        return new TestRunOutcome(result, process.ExitCode, failed, output);
    }

    // The CTRF report (JSON) lists every test with its full name and status.
    private static List<string> FailedTests(string reportPath)
    {
        if (!File.Exists(reportPath))
        {
            return [];
        }

        using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
        return
        [
            .. report.RootElement.GetProperty("results").GetProperty("tests").EnumerateArray()
                .Where(t => t.GetProperty("status").GetString() == "failed")
                .Select(t => t.GetProperty("name").GetString() ?? "(unnamed test)"),
        ];
    }
}
