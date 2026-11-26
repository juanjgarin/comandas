using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Menu;

/// <summary>
/// A simple menu item with its own price (a "leaf" in the Composite pattern introduced with <c>Combo</c>).
/// </summary>
public sealed class Dish : MenuItem
{
    private Dish(Guid id, string name, Money basePrice)
        : base(id, name)
    {
        BasePrice = ValidatePrice(basePrice);
    }

    // Parameterless constructor for EF Core materialization only.
    private Dish()
    {
        BasePrice = null!; // Set by EF Core when loading from the database.
    }

    public Money BasePrice { get; private set; }

    public static Dish Create(string name, Money basePrice) =>
        new(Guid.CreateVersion7(), name, basePrice);

    public override Money GetPrice() => BasePrice;

    public void ChangePrice(Money newPrice) => BasePrice = ValidatePrice(newPrice);

    private static Money ValidatePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (price.Amount == 0)
        {
            throw new DomainException("A dish must have a price greater than zero.");
        }

        return price;
    }
}
