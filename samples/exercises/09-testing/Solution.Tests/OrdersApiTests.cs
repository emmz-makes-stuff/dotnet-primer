using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TestingOrders;

namespace TestingOrders.Tests;

/// <summary>
/// The whole app, in memory, with the clock and the warehouse replaced. Requests go through routing,
/// model binding and the app's real service registrations.
/// </summary>
public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));

    public IWarehouse Warehouse { get; } = Substitute.For<IWarehouse>();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.RemoveAll<IWarehouse>();
            services.AddSingleton(Warehouse);
        });
}

// TODO 4 (solved): a request-to-response test catches what unit tests can't see: routing, binding,
// status codes, and how the app's services are registered.
public sealed class OrdersApiTests : IDisposable
{
    private static readonly PlaceOrderRequest _valid = new("ada@example.com", [new("SKU-1", 2, 1_250)]);

    private readonly OrdersApiFactory _factory = new();
    private readonly HttpClient _client;

    public OrdersApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task PlacedOrderCanBeReadBack()
    {
        var ct = TestContext.Current.CancellationToken;

        var placed = await _client.PostAsJsonAsync("/orders", _valid, ct);
        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
        var created = await placed.Content.ReadFromJsonAsync<Order>(ct);

        // A second request: it only finds the order if every request shares the same store.
        var read = await _client.GetAsync(placed.Headers.Location, ct);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var order = await read.Content.ReadFromJsonAsync<Order>(ct);
        Assert.Equal(created!.Id, order!.Id);
        Assert.Equal(2_500, order.TotalCents);
    }

    [Fact]
    public async Task InvalidOrderIsABadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync("/orders", _valid with { Lines = [] }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownOrderIsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync("/orders/999", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelAfterTheWindowIsAConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var placed = await _client.PostAsJsonAsync("/orders", _valid, ct);
        _factory.Time.Advance(TimeSpan.FromMinutes(31));

        var response = await _client.PostAsync($"{placed.Headers.Location}/cancel", null, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
