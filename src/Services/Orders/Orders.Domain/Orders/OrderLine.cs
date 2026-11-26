using Comandas.Orders.Domain.Menu;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Orders;

/// <summary>
/// One line of an <see cref="Order"/>: "2 × Burger at 10.00 EUR".
/// It stores a SNAPSHOT of the item's name and price at the moment it was ordered: later menu changes
/// must not alter what was already sold. Only the <see cref="Order"/> aggregate can create or change it.
/// </summary>
public sealed class OrderLine : Entity<Guid>
{
    internal OrderLine(MenuItem item, Quantity quantity)
        : base(Guid.CreateVersion7())
    {
        MenuItemId = item.Id;
        ItemName = item.Name;
        UnitPrice = item.GetPrice();
        IsTwoForOneEligible = item.IsTwoForOneEligible;
        Quantity = quantity;
    }

    // Parameterless constructor for EF Core materialization only.
    private OrderLine()
    {
        ItemName = string.Empty; // Set by EF Core when loading from the database.
        UnitPrice = null!;
        Quantity = null!;
    }

    /// <summary>Reference to the menu item by id only: the line does not hold the MenuItem object.</summary>
    public Guid MenuItemId { get; private set; }

    public string ItemName { get; private set; }

    public Money UnitPrice { get; private set; }

    /// <summary>Snapshot of the item's 2-for-1 flag when ordered: later menu changes do not affect this line.</summary>
    public bool IsTwoForOneEligible { get; private set; }

    public Quantity Quantity { get; private set; }

    public Money LineTotal => UnitPrice * Quantity.Value;

    internal void ChangeQuantity(Quantity newQuantity) => Quantity = newQuantity;

    internal void Increase(Quantity extra) => Quantity += extra;
}
