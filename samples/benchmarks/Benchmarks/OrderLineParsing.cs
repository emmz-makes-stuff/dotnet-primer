using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace Primer.Benchmarks;

/// <summary>
/// Parsing an order line like <c>SKU-1,2,1250</c> (SKU, quantity, price in cents) into its total.
/// <see cref="string.Split(char, StringSplitOptions)"/> allocates an array and a string for each
/// part; slicing a <see cref="ReadOnlySpan{T}"/> reads the same characters where they are.
/// </summary>
[MemoryDiagnoser]
public class OrderLineParsing
{
    private string _line = "";

    [GlobalSetup]
    public void Setup() => _line = "SKU-1,2,1250";

    [Benchmark(Baseline = true)]
    public long Split()
    {
        var parts = _line.Split(',');
        if (!parts[0].StartsWith("SKU-", StringComparison.Ordinal))
        {
            return -1;
        }

        return int.Parse(parts[1], CultureInfo.InvariantCulture) * long.Parse(parts[2], CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public long Span()
    {
        var line = _line.AsSpan();
        var first = line.IndexOf(',');
        var sku = line[..first];
        var rest = line[(first + 1)..];
        var second = rest.IndexOf(',');
        if (!sku.StartsWith("SKU-", StringComparison.Ordinal))
        {
            return -1;
        }

        return int.Parse(rest[..second], CultureInfo.InvariantCulture) * long.Parse(rest[(second + 1)..], CultureInfo.InvariantCulture);
    }
}
