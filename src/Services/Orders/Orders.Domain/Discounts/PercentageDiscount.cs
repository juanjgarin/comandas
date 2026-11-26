using System.Globalization;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Discounts;

/// <summary>A fixed percentage off the whole order, e.g. 10% (a concrete Strategy).</summary>
public sealed class PercentageDiscount : IDiscountStrategy
{
    public PercentageDiscount(decimal percentage)
    {
        if (percentage is <= 0 or > 100)
        {
            throw new DomainException(
                $"A percentage discount must be greater than 0% and at most 100% (was {percentage.ToString(CultureInfo.InvariantCulture)}%).");
        }

        Percentage = percentage;
    }

    public decimal Percentage { get; }

    public string Description => string.Create(CultureInfo.InvariantCulture, $"{Percentage:0.##}% off");

    public Money CalculateDiscount(DiscountContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Subtotal * (Percentage / 100m);
    }
}
