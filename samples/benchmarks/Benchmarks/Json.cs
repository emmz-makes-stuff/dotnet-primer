using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;

namespace Primer.Benchmarks;

/// <summary>A line of an order, as the exercises have it.</summary>
public sealed record OrderLine(string Sku, int Quantity, long PriceCents);

/// <summary>An order to serialize.</summary>
public sealed record Order(int Id, IReadOnlyList<OrderLine> Lines);

/// <summary>Serialization code for <see cref="Order"/>, written at build time by the JSON source generator.</summary>
[JsonSerializable(typeof(Order))]
internal sealed partial class OrderJsonContext : JsonSerializerContext;

/// <summary>
/// Serializing an order to JSON. The default serializer works out each type's shape by reflection
/// (once, then caches it); the source-generated <see cref="JsonSerializerContext"/> has it already,
/// and works with trimming and Native AOT.
/// </summary>
[MemoryDiagnoser]
public class Json
{
    private Order _order = new(0, []);

    [GlobalSetup]
    public void Setup() =>
        _order = new Order(42, [.. Enumerable.Range(1, 10).Select(i => new OrderLine($"SKU-{i}", i, 100L * i))]);

    [Benchmark(Baseline = true)]
    public string Reflection() => JsonSerializer.Serialize(_order);

    [Benchmark]
    public string SourceGenerated() => JsonSerializer.Serialize(_order, OrderJsonContext.Default.Order);
}
