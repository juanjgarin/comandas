using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// "2-for-1" on items flagged as eligible (a concrete Strategy). Counted PER LINE: every eligible line gives
/// floor(quantity / 2) units for free at its own unit price. 3 sodas -> 1 free; 1 soda -> none.
/// </summary>
/// <remarks>
/// It reads each line's SNAPSHOT of the flag (<see cref="Orders.OrderLine.IsTwoForOneEligible"/>), not the
/// current menu, so switching the promotion off later does not change orders already taken.
/// </remarks>
public sealed class TwoForOneDiscount : IDiscountStrategy
{
    public string Description => "2-for-1 on selected items";

    public Money CalculateDiscount(DiscountContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Lines
            .Where(line => line.IsTwoForOneEligible)
            .Select(line => line.UnitPrice * (line.Quantity.Value / 2)) // integer division: 3 / 2 = 1 free unit
            .Aggregate(Money.Zero(context.Subtotal.Currency), (total, free) => total + free);
    }
}
