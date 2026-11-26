namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// Factory pattern: chooses and builds the discount strategy for an order. Callers get an
/// <see cref="IDiscountStrategy"/> and never <c>new</c> a concrete discount themselves (Dependency Inversion);
/// strategies calculate, the factory selects (Single Responsibility).
/// </summary>
public interface IDiscountFactory
{
    /// <summary>The single best discount for this order right now; <see cref="NoDiscount"/> when none applies.</summary>
    IDiscountStrategy CreateBestFor(DiscountContext context, DiscountRequest request);
}
