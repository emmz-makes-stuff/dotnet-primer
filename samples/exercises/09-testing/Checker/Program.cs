using TestingOrders.Checker;

// The checker for exercise 09. It runs a test project against the correct API, then against each
// seeded bug, and reports which bugs the tests caught. Nothing leaves the machine.
//
//   dotnet run --project Checker                    checks Starter.Tests (your tests)
//   dotnet run --project Checker -- Solution.Tests  checks another test project
//
// Exit code: 0 when every seeded bug is caught, 1 otherwise.

var exerciseRoot = FindExerciseRoot();
var testProject = FindTestProject(args.Length > 0 ? args[0] : "Starter.Tests", exerciseRoot);
if (testProject is null)
{
    return 1;
}

var problems = DriftProblems(exerciseRoot);
if (problems.Count > 0)
{
    Console.WriteLine("The exercise's seeded bugs are out of step with the app (this is a fault in the exercise, not your tests):");
    problems.ForEach(p => Console.WriteLine($"  {p}"));
    return 1;
}

// Keyed by the test project's folder, so two copies of one project don't share builds.
var runsRoot = Path.Combine(exerciseRoot, "Checker", "obj", "runs", Path.GetFileName(Path.GetDirectoryName(testProject)!));
Console.WriteLine($"Checking {Path.GetRelativePath(exerciseRoot, testProject)}.");
Console.WriteLine();
Console.Write("Against the correct API ... ");

var correct = await TestRun.RunAsync(testProject, exerciseRoot, Path.Combine(runsRoot, "correct"), bug: null);
switch (correct.Result)
{
    case RunResult.Error:
        Console.WriteLine($"couldn't build or run the tests (exit code {correct.ExitCode}):");
        Console.WriteLine(correct.Output);
        return 1;

    case RunResult.TimedOut:
        Console.WriteLine(TimedOutMessage());
        return 1;

    case RunResult.NoTests:
        Console.WriteLine("no tests yet.");
        Console.WriteLine();
        Console.WriteLine("Write some tests, starting with TODO 1 in PricingTests.cs. Until then, every seeded bug is missed:");
        foreach (var bug in SeededBug.All)
        {
            Console.WriteLine($"  Bug {bug.Number} ({bug.Name}): missed. {bug.Hint}");
        }

        return 1;

    case RunResult.TestsFailed:
        Console.WriteLine("some tests fail.");
        Console.WriteLine();
        Console.WriteLine("These tests fail against the correct API. The API is right, so the tests are wrong:");
        foreach (var test in correct.FailedTests)
        {
            Console.WriteLine($"  {Short(test)}");
        }

        Console.WriteLine();
        Console.WriteLine($"Run them on their own for the details: dotnet test --project {Path.GetRelativePath(exerciseRoot, testProject)}");
        Console.WriteLine("The seeded bugs weren't tried. Fix these tests first.");
        return 1;

    default:
        Console.WriteLine("all your tests pass.");
        break;
}

var caught = 0;
foreach (var bug in SeededBug.All)
{
    Console.Write($"Against bug {bug.Number} ({bug.Name}) ... ");
    var run = await TestRun.RunAsync(testProject, exerciseRoot, Path.Combine(runsRoot, $"bug-{bug.Number}"), bug);
    switch (run.Result)
    {
        case RunResult.TestsFailed:
            caught++;
            // The names come from the report file; without it, the exit code still says a test failed.
            Console.WriteLine(run.FailedTests.Count > 0 ? $"caught, by {string.Join(", ", run.FailedTests.Select(Short))}." : "caught.");
            break;
        case RunResult.Passed:
            Console.WriteLine("missed: every test still passes.");
            Console.WriteLine($"  Hint: {bug.Hint}");
            break;
        case RunResult.TimedOut:
            Console.WriteLine(TimedOutMessage());
            return 1;
        default:
            // Your tests already passed against the correct API, so this is rare. A test can still
            // crash the test host against a bug, or the exercise itself can be at fault.
            Console.WriteLine($"stopped without a result (exit code {run.ExitCode}).");
            Console.WriteLine("  A test may have crashed the test run, or the exercise may be at fault. The run's output:");
            Console.WriteLine(run.Output);
            return 1;
    }
}

Console.WriteLine();
Console.WriteLine($"{caught} of {SeededBug.All.Length} seeded bugs caught.");
return caught == SeededBug.All.Length ? 0 : 1;

static string TimedOutMessage() =>
    $"didn't finish within {TestRun.Timeout.TotalMinutes:0} minutes, so the checker stopped it.\n" +
    "  A test is probably waiting for something that never happens, such as a call that's never\n" +
    "  made. Give that wait a time limit, so the test fails instead of hanging.";

// A test's name without the tests' shared namespace, which only makes the report longer.
static string Short(string testName) =>
    testName.StartsWith("TestingOrders.Tests.", StringComparison.Ordinal) ? testName["TestingOrders.Tests.".Length..] : testName;

// The exercise folder is the one that holds bugs/SeededBugs.targets, above the checker's output.
static string FindExerciseRoot()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "bugs", "SeededBugs.targets")))
        {
            return dir.FullName;
        }
    }

    throw new InvalidOperationException("Couldn't find the exercise folder (the one with bugs/SeededBugs.targets).");
}

// A folder relative to the current directory or to the exercise, holding exactly one .csproj.
static string? FindTestProject(string folder, string exerciseRoot)
{
    var directory = Directory.Exists(folder) ? Path.GetFullPath(folder) : Path.Combine(exerciseRoot, folder);
    var projects = Directory.Exists(directory) ? Directory.GetFiles(directory, "*.csproj") : [];
    if (projects.Length == 1)
    {
        return projects[0];
    }

    Console.WriteLine($"There's no single test project in '{folder}'. Pass a folder with one .csproj, such as Starter.Tests.");
    return null;
}

// Each bug file must be its App file with exactly one line changed, and that line must carry the
// bug's marker. Anything else means the App changed and the bug wasn't updated with it.
static List<string> DriftProblems(string exerciseRoot)
{
    var problems = new List<string>();
    foreach (var bug in SeededBug.All)
    {
        var appLines = File.ReadAllLines(Path.Combine(exerciseRoot, "App", bug.File));
        var bugPath = Path.Combine("bugs", bug.Folder, bug.File);
        var bugLines = File.ReadAllLines(Path.Combine(exerciseRoot, bugPath));
        if (appLines.Length != bugLines.Length)
        {
            problems.Add($"{bugPath} has {bugLines.Length} lines, but App/{bug.File} has {appLines.Length}.");
            continue;
        }

        var changed = Enumerable.Range(0, appLines.Length).Where(i => appLines[i] != bugLines[i]).ToList();
        if (changed.Count != 1 || !bugLines[changed[0]].Contains(bug.Marker, StringComparison.Ordinal))
        {
            problems.Add($"{bugPath} must differ from App/{bug.File} in exactly one line, ending with '{bug.Marker}'. It differs in {changed.Count}.");
        }
    }

    return problems;
}
