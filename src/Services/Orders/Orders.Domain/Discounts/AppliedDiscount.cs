using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// The discount an order got, frozen at the moment it was applied (Value Object): what the bill shows
/// and how much it saved. The strategy that produced it is NOT stored: strategies are behavior and can
/// depend on the clock (happy hour), while the bill must not change by itself after the fact.
/// </summary>
public sealed class AppliedDiscount : ValueObject
{
    public const int DescriptionMaxLength = 100;

    private AppliedDiscount(string description, Money amount)
    {
        Description = description;
        Amount = amount;
    }

    // Parameterless constructor for EF Core materialization only (an owned Money cannot be bound
    // through a constructor parameter).
    private AppliedDiscount()
    {
        Description = string.Empty; // Set by EF Core when loading from the database.
        Amount = null!; // Set by EF Core: the owned Money navigation is required when the discount exists.
    }

    /// <summary>Label shown on the bill, e.g. "Happy hour: 20% off".</summary>
    public string Description { get; }

    /// <summary>How much is subtracted from the subtotal. Always greater than zero.</summary>
    public Money Amount { get; }

    public static AppliedDiscount Of(string description, Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("A discount needs a description to show on the bill.");
        }

        if (description.Length > DescriptionMaxLength)
        {
            throw new DomainException($"A discount description cannot exceed {DescriptionMaxLength} characters.");
        }

        if (amount.Amount <= 0)
        {
            throw new DomainException("An applied discount must save something; a zero discount is no discount.");
        }

        return new AppliedDiscount(description.Trim(), amount);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Description;
        yield return Amount;
    }
}
