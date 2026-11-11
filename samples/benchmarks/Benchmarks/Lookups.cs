using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;

namespace Primer.Benchmarks;

/// <summary>
/// Looking up 100 prices by SKU. A <see cref="FrozenDictionary{TKey, TValue}"/> can't change after
/// it's made, so it spends longer building itself, and in return looks keys up faster: right for
/// data that's loaded once and read on every request.
/// </summary>
[MemoryDiagnoser]
public class Lookups
{
    private string[] _keys = [];
    private Dictionary<string, long> _dictionary = [];
    private FrozenDictionary<string, long> _frozen = FrozenDictionary<string, long>.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _keys = [.. Enumerable.Range(1, 100).Select(i => $"SKU-{i}")];
        _dictionary = _keys.ToDictionary(key => key, key => (long)key.Length * 100);
        _frozen = _dictionary.ToFrozenDictionary();
    }

    [Benchmark(Baseline = true)]
    public long Dictionary()
    {
        var total = 0L;
        foreach (var key in _keys)
        {
            total += _dictionary[key];
        }

        return total;
    }

    [Benchmark]
    public long Frozen()
    {
        var total = 0L;
        foreach (var key in _keys)
        {
            total += _frozen[key];
        }

        return total;
    }
}
