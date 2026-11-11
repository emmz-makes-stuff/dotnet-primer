using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TestingOrders;

namespace TestingOrders.Tests;

// TODO 2 (solved): the warehouse is a dependency, so the test replaces it with a double and checks
// what the service told it. Here the double is an NSubstitute substitute; a small hand-written fake
// that records its calls works just as well.
public class CancelReleasesStockTests
{
    private static readonly OrderLine[] _lines = [new("SKU-1", 2, 1_250), new("SKU-2", 1, 500)];

    private readonly IWarehouse _warehouse = Substitute.For<IWarehouse>();
    private readonly FakeTimeProvider _time = new();
    private readonly OrderStore _store = new();
    private readonly OrderService _orders;

    public CancelReleasesStockTests() => _orders = new OrderService(_store, _warehouse, _time);

    [Fact]
    public async Task CancellingReleasesTheOrdersLines()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = _orders.Place("ada@example.com", _lines);

        var outcome = await _orders.CancelAsync(order.Id, ct);

        Assert.Equal(CancelOutcome.Cancelled, outcome);
        // Check the arguments, not just that a call happened.
        await _warehouse.Received(1).ReleaseAsync(
            order.Id, Arg.Is<IReadOnlyList<OrderLine>>(lines => lines.SequenceEqual(_lines)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailedReleaseLeavesTheOrderPlaced()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = _orders.Place("ada@example.com", _lines);
        _warehouse.ReleaseAsync(default, default!, ct)
            .ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("The warehouse is down.")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _orders.CancelAsync(order.Id, ct));

        // Still placed, so the customer can try the cancel again.
        Assert.Equal(OrderStatus.Placed, _store.Find(order.Id)?.Status);
    }

    [Fact]
    public async Task RefusedCancelReleasesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = _orders.Place("ada@example.com", _lines);
        _time.Advance(TimeSpan.FromHours(1));

        var outcome = await _orders.CancelAsync(order.Id, ct);

        Assert.Equal(CancelOutcome.WindowClosed, outcome);
        // WithAnyArgs: the arguments below are placeholders; no call at all may have been made.
        await _warehouse.DidNotReceiveWithAnyArgs().ReleaseAsync(default, default!, ct);
    }
}
