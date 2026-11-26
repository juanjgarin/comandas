using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// Null Object pattern: the "discount" used when none applies. Callers always get a usable strategy and never
/// need <c>if (discount is null)</c>.
/// </summary>
public sealed class NoDiscount : IDiscountStrategy
{
    public static NoDiscount Instance { get; } = new();

    private NoDiscount()
    {
    }

    public string Description => "No discount";

    public Money CalculateDiscount(DiscountContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Money.Zero(context.Subtotal.Currency);
    }
}
