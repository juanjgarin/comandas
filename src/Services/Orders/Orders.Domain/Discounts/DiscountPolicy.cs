namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// The restaurant's discount settings, as plain data. The domain receives this record instead of depending on
/// the .NET configuration system; the Application layer builds it from configuration.
/// </summary>
public sealed record DiscountPolicy(TimeOnly HappyHourStart, TimeOnly HappyHourEnd, decimal HappyHourPercentage);
