using System.Text;
using BenchmarkDotNet.Attributes;

namespace Primer.Benchmarks;

/// <summary>
/// Joining 100 SKUs with commas. <c>+=</c> copies the whole string so far every time round the loop;
/// <see cref="StringBuilder"/> grows one buffer; <see cref="string.Create{TState}"/> works out the
/// length first, and writes the characters once, straight into the new string.
/// </summary>
[MemoryDiagnoser]
public class StringBuilding
{
    private string[] _skus = [];

    [GlobalSetup]
    public void Setup() => _skus = [.. Enumerable.Range(1, 100).Select(i => $"SKU-{i}")];

    [Benchmark(Baseline = true)]
    public string Concatenate()
    {
        var result = "";
        for (var i = 0; i < _skus.Length; i++)
        {
            // One string.Concat(result, ",", sku) per round, so the only cost measured is copying
            // the string so far. Not `result += "," + sku`: that makes a temporary string as well.
            result = i == 0 ? _skus[i] : result + "," + _skus[i];
        }

        return result;
    }

    [Benchmark]
    public string Builder()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _skus.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(_skus[i]);
        }

        return builder.ToString();
    }

    [Benchmark]
    public string Create()
    {
        var length = _skus.Length - 1;
        foreach (var sku in _skus)
        {
            length += sku.Length;
        }

        return string.Create(length, _skus, static (span, skus) =>
        {
            var at = 0;
            for (var i = 0; i < skus.Length; i++)
            {
                if (i > 0)
                {
                    span[at++] = ',';
                }

                skus[i].AsSpan().CopyTo(span[at..]);
                at += skus[i].Length;
            }
        });
    }
}
