using Microsoft.Extensions.Time.Testing;
using TestingOrders;

namespace TestingOrders.Tests;

// TODO 3 (solved): the rule depends on the time, so the test controls the time with a
// FakeTimeProvider instead of reading the clock or waiting 30 minutes.
public class CancelWindowTests
{
    private static readonly DateTimeOffset _placedAt = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(_placedAt);
    private readonly Order _order = new(1, "ada@example.com", [new("SKU-1", 1, 500)], 500, _placedAt, OrderStatus.Placed);

    [Fact]
    public void OrderCanBeCancelledRightAway() => Assert.True(CancelPolicy.CanCancel(_order, _time));

    [Fact]
    public void OrderCanBeCancelledAtTheEndOfTheWindow()
    {
        _time.Advance(TimeSpan.FromMinutes(30));

        Assert.True(CancelPolicy.CanCancel(_order, _time));
    }

    [Fact]
    public void OrderCantBeCancelledAfterTheWindow()
    {
        _time.Advance(TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(1));

        Assert.False(CancelPolicy.CanCancel(_order, _time));
    }
}
