using ContainerOrders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// TODO 1: This pins the app to port 5000. In a container, the port is the host's choice: the .NET
//   images set ASPNETCORE_HTTP_PORTS to 8080, and whoever runs the container can change it. Remove
//   this line, so the app listens where it's told.
builder.WebHost.UseUrls("http://+:5000");

// TODO 2: This reads appsettings.json on its own, so no environment variable can change the
//   warehouse name, and in a container, environment variables are how settings arrive. Bind
//   WarehouseOptions from builder.Configuration instead: it reads appsettings.json and then the
//   environment, so Warehouse__Name overrides the file.
var settings = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();
builder.Services.AddOptions<WarehouseOptions>()
    .Bind(settings.GetSection(WarehouseOptions.Section))
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
