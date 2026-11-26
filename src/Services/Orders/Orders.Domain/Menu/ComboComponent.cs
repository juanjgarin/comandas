using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Menu;

/// <summary>
/// One entry of a <see cref="Combo"/>: "2 × Burger". Part of the Combo aggregate, so only the combo
/// can create or change it (hence the <c>internal</c> members).
/// </summary>
public sealed class ComboComponent : Entity<Guid>
{
    internal ComboComponent(MenuItem item, Quantity quantity)
        : base(Guid.CreateVersion7())
    {
        Item = item;
        Quantity = quantity;
    }

    // Parameterless constructor for EF Core materialization only.
    private ComboComponent()
    {
        Item = null!;     // Set by EF Core when loading from the database.
        Quantity = null!;
    }

    public MenuItem Item { get; private set; }

    public Quantity Quantity { get; private set; }

    /// <summary>Price of this entry: the item's own price (which may itself be a combo) times the quantity.</summary>
    public Money GetSubtotal() => Item.GetPrice() * Quantity.Value;

    internal void Increase(Quantity extra) => Quantity += extra;
}
