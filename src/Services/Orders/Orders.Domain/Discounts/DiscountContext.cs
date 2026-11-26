using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// The read-only data a discount strategy may look at. Strategies receive this instead of the Order itself,
/// so they cannot change the order (Interface Segregation: they get only what they read). New data needed
/// by a future discount is added here, without changing the signature of every strategy.
/// </summary>
public sealed record DiscountContext(Money Subtotal, IReadOnlyCollection<OrderLine> Lines)
{
    public static DiscountContext For(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return new DiscountContext(order.Subtotal, order.Lines);
    }
}
