namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// What the waiter explicitly asks for when applying a discount. Automatic discounts (happy hour, 2-for-1)
/// need no request; a manual percentage (e.g. staff discount) does.
/// </summary>
public sealed record DiscountRequest(decimal? ManualPercentage = null)
{
    public static DiscountRequest None { get; } = new();
}
