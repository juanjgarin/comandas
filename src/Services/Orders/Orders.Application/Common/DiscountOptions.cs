using Comandas.Orders.Domain.Discounts;

namespace Comandas.Orders.Application.Common;

/// <summary>Discount settings, bound from the "Discounts" configuration section (Options pattern).</summary>
public sealed class DiscountOptions
{
    public const string SectionName = "Discounts";

    /// <summary>Start of the daily happy hour window, server local time (included).</summary>
    public TimeOnly HappyHourStart { get; init; } = new(18, 0);

    /// <summary>End of the daily happy hour window, server local time (excluded).</summary>
    public TimeOnly HappyHourEnd { get; init; } = new(20, 0);

    public decimal HappyHourPercentage { get; init; } = 20m;

    /// <summary>The domain's view of these settings: plain data, no configuration framework.</summary>
    public DiscountPolicy ToPolicy() => new(HappyHourStart, HappyHourEnd, HappyHourPercentage);
}
