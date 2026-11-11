using ContainerOrders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// TODO 1 (solved): no UseUrls. The app listens where ASPNETCORE_HTTP_PORTS says, which the .NET
// images set to 8080, and which whoever runs the container can change.

// TODO 2 (solved): the warehouse settings come from the host's configuration, which reads
// appsettings.json and then environment variables. So Warehouse__Name overrides the file.
builder.Services.AddOptions<WarehouseOptions>()
    .Bind(builder.Configuration.GetSection(WarehouseOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<OrderStore>();

var app = builder.Build();

var warehouseName = app.Services.GetRequiredService<IOptions<WarehouseOptions>>().Value.Name;
Log.Warehouse(app.Logger, warehouseName);

app.MapGet("/info", (IOptions<WarehouseOptions> warehouse) => new { warehouse = warehouse.Value.Name });

app.MapGet("/orders", (OrderStore store) => store.All());

app.MapPost("/orders", (PlaceOrderRequest request, OrderStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Sku) || request.Quantity is < 1 or > 1_000)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["order"] = ["An order needs a SKU, and a quantity from 1 to 1000."],
        });
    }

    var order = store.Add(request.Sku, request.Quantity);
    return Results.Created($"/orders/{order.Id}", order);
});

app.Run();

internal static partial class Log
{
    /// <summary>The tests read this line to see which warehouse name the app ended up with.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Warehouse: {WarehouseName}")]
    public static partial void Warehouse(ILogger logger, string warehouseName);
}
