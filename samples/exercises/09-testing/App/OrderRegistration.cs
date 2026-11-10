namespace TestingOrders;

/// <summary>Registers the orders API's services.</summary>
public static class OrderRegistration
{
    /// <summary>Adds the store, the service, the warehouse and the clock the API uses.</summary>
    public static IServiceCollection AddOrders(this IServiceCollection services)
    {
        // One store for the life of the app: it holds every order in memory.
        services.AddSingleton<OrderStore>();
        services.AddScoped<OrderService>();
        services.AddSingleton<IWarehouse, LoggingWarehouse>();

        // The real clock. Tests replace it with a FakeTimeProvider they can move forward.
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
