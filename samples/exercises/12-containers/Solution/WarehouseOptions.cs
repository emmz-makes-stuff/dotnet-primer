using System.ComponentModel.DataAnnotations;

namespace ContainerOrders;

/// <summary>Which warehouse the app sends orders to: the <c>Warehouse</c> section of configuration.</summary>
public sealed class WarehouseOptions
{
    public const string Section = "Warehouse";

    /// <summary>
    /// The warehouse's name. <c>appsettings.json</c> sets a default; in a container, the
    /// <c>Warehouse__Name</c> environment variable should override it.
    /// </summary>
    [Required]
    public string Name { get; set; } = "";
}
