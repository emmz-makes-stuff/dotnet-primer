using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Primer.Benchmarks;

/// <summary>
/// A scratch buffer for each call: a new array every time, or one rented from
/// <see cref="ArrayPool{T}.Shared"/> and given back. 100 000 bytes is over the 85 000-byte line, so
/// a new array of that size goes on the large object heap.
/// </summary>
[MemoryDiagnoser]
public class Buffers
{
    private byte[] _payload = [];

    [Params(1_024, 100_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[Size];
        Random.Shared.NextBytes(_payload);
    }

    [Benchmark(Baseline = true)]
    public int NewArray()
    {
        var buffer = new byte[Size];
        _payload.CopyTo(buffer, 0);
        return Checksum(buffer.AsSpan(0, Size));
    }

    [Benchmark]
    public int Pooled()
    {
        // A rented array can be bigger than asked for, and holds whatever the last renter left.
        var buffer = ArrayPool<byte>.Shared.Rent(Size);
        try
        {
            _payload.CopyTo(buffer, 0);
            return Checksum(buffer.AsSpan(0, Size));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int Checksum(ReadOnlySpan<byte> bytes)
    {
        var sum = 0;
        foreach (var b in bytes)
        {
            sum += b;
        }

        return sum;
    }
}
