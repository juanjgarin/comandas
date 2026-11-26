using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// Strategy pattern: one interchangeable algorithm per kind of discount (percentage, happy hour, 2-for-1...).
/// The order only knows "there is a discount that calculates an amount"; adding a new kind of discount means
/// adding a new class, never editing the order (Open/Closed Principle).
/// </summary>
public interface IDiscountStrategy
{
    /// <summary>Human-readable label shown on the bill, e.g. "10% off".</summary>
    string Description { get; }

    /// <summary>
    /// The amount to subtract. Never negative and never more than the subtotal. Zero when the discount
    /// does not apply (e.g. outside happy hour). Pure calculation: it reads the context, changes nothing.
    /// </summary>
    Money CalculateDiscount(DiscountContext context);
}
