namespace Comandas.Orders.Application.Common;

/// <summary>Restaurant-wide settings, bound from the "Restaurant" configuration section (Options pattern).</summary>
public sealed class RestaurantOptions
{
    public const string SectionName = "Restaurant";

    /// <summary>ISO 4217 currency every new order uses.</summary>
    public string Currency { get; init; } = "EUR";
}
