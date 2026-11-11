using BenchmarkDotNet.Running;

// Run in Release, from the repo root:
//   dotnet run -c Release --project samples/benchmarks/Benchmarks -- --filter '*'
// --filter picks benchmarks: '*' runs them all, '*Lookups*' runs one class. With no arguments, it
// asks which to run.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
