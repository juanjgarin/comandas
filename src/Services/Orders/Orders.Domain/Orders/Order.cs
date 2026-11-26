using Comandas.Orders.Domain.Menu;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Orders;

/// <summary>
/// A table's order (aggregate root). The only entry point to change the order and its lines,
/// so every rule about lines, quantities and totals is enforced here, in one place.
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    public const int MinTableNumber = 1;
    public const int MaxTableNumber = 999;

    private readonly List<OrderLine> _lines = [];

    private Order(Guid id, int tableNumber, string currency)
        : base(id)
    {
        TableNumber = tableNumber;
        Currency = currency;
    }

    // Parameterless constructor for EF Core materialization only.
    private Order()
    {
        Currency = string.Empty; // Set by EF Core when loading from the database.
    }

    public int TableNumber { get; private set; }

    /// <summary>Every line of the order must be priced in this currency.</summary>
    public string Currency { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>Sum of all line totals, before discounts (discounts arrive in phase 2).</summary>
    public Money Subtotal => _lines.Aggregate(Money.Zero(Currency), (total, line) => total + line.LineTotal);

    public static Order Create(int tableNumber, string currency)
    {
        if (tableNumber is < MinTableNumber or > MaxTableNumber)
        {
            throw new DomainException($"Table number must be between {MinTableNumber} and {MaxTableNumber} (was {tableNumber}).");
        }

        // Money already knows how to validate and normalize an ISO currency code: reuse it instead of duplicating the rule.
        var normalizedCurrency = Money.Zero(currency).Currency;

        return new Order(Guid.CreateVersion7(), tableNumber, normalizedCurrency);
    }

    /// <summary>
    /// Adds an item to the order. If the same item with the same price is already there, its quantity grows
    /// instead of creating a second line.
    /// </summary>
    public OrderLine AddItem(MenuItem item, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(quantity);

        if (!item.CanBeOrdered)
        {
            throw new DomainException($"'{item.Name}' cannot be ordered right now.");
        }

        var price = item.GetPrice();
        if (price.Currency != Currency)
        {
            throw new DomainException($"'{item.Name}' is priced in {price.Currency} but the order uses {Currency}.");
        }

        // Merge only when the snapshot price matches: if the menu price changed in between,
        // the new units are sold at the new price, on their own line.
        var existing = _lines.Find(line => line.MenuItemId == item.Id && line.UnitPrice == price);
        if (existing is not null)
        {
            existing.Increase(quantity);
            return existing;
        }

        var newLine = new OrderLine(item, quantity);
        _lines.Add(newLine);
        return newLine;
    }

    public void ChangeLineQuantity(Guid lineId, Quantity newQuantity)
    {
        ArgumentNullException.ThrowIfNull(newQuantity);

        GetLine(lineId).ChangeQuantity(newQuantity);
    }

    public void RemoveLine(Guid lineId) => _lines.Remove(GetLine(lineId));

    private OrderLine GetLine(Guid lineId) =>
        _lines.Find(line => line.Id == lineId)
        ?? throw new DomainException($"Order {Id} has no line {lineId}.");
}
