using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders.Events;
using Comandas.Orders.Domain.Orders.States;
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
        Status = OrderStatus.Created;
    }

    // Parameterless constructor for EF Core materialization only.
    private Order()
    {
        Currency = string.Empty; // Set by EF Core when loading from the database.
    }

    public int TableNumber { get; private set; }

    /// <summary>Every line of the order must be priced in this currency.</summary>
    public string Currency { get; private set; }

    /// <summary>Where the order is in its lifecycle. Persisted as-is; the behavior comes from <see cref="State"/>.</summary>
    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>Sum of all line totals, before discounts (discounts arrive in phase 2).</summary>
    public Money Subtotal => _lines.Aggregate(Money.Zero(Currency), (total, line) => total + line.LineTotal);

    // State pattern: the object that knows what is allowed in the current status.
    private OrderState State => OrderState.From(Status);

    public static Order Create(int tableNumber, string currency)
    {
        if (tableNumber is < MinTableNumber or > MaxTableNumber)
        {
            throw new DomainException($"Table number must be between {MinTableNumber} and {MaxTableNumber} (was {tableNumber}).");
        }

        // Money already knows how to validate and normalize an ISO currency code: reuse it instead of duplicating the rule.
        var normalizedCurrency = Money.Zero(currency).Currency;

        var order = new Order(Guid.CreateVersion7(), tableNumber, normalizedCurrency);
        order.Raise(new OrderCreatedDomainEvent(order.Id, order.TableNumber));
        return order;
    }

    /// <summary>
    /// Adds an item to the order. If the same item with the same price is already there, its quantity grows
    /// instead of creating a second line.
    /// </summary>
    public OrderLine AddItem(MenuItem item, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(quantity);
        EnsureLinesCanBeEdited();

        if (!item.CanBeOrdered)
        {
            throw new DomainException($"'{item.Name}' cannot be ordered right now.");
        }

        var price = item.GetPrice();
        if (price.Currency != Currency)
        {
            throw new DomainException($"'{item.Name}' is priced in {price.Currency} but the order uses {Currency}.");
        }

        // Merge only when the whole snapshot matches (price and 2-for-1 flag): if the menu changed in between,
        // the new units are sold under the new conditions, on their own line.
        var existing = _lines.Find(line =>
            line.MenuItemId == item.Id
            && line.UnitPrice == price
            && line.IsTwoForOneEligible == item.IsTwoForOneEligible);
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
        EnsureLinesCanBeEdited();

        GetLine(lineId).ChangeQuantity(newQuantity);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureLinesCanBeEdited();

        _lines.Remove(GetLine(lineId));
    }

    // --- Lifecycle (State pattern): Order delegates; the current state decides or throws. ---
    // Each successful transition announces itself with a domain event (Observer). The event is raised
    // AFTER the transition, so a rejected transition announces nothing.

    public void Confirm()
    {
        if (_lines.Count == 0)
        {
            throw new DomainException("Cannot confirm an order without lines.");
        }

        TransitionTo(State.Confirm());
        Raise(new OrderConfirmedDomainEvent(Id, TableNumber));
    }

    public void StartPreparation()
    {
        TransitionTo(State.StartPreparation());
        Raise(new OrderPreparationStartedDomainEvent(Id));
    }

    public void MarkReady()
    {
        TransitionTo(State.MarkReady());
        Raise(new OrderReadyDomainEvent(Id, TableNumber));
    }

    public void Deliver()
    {
        TransitionTo(State.Deliver());
        Raise(new OrderDeliveredDomainEvent(Id));
    }

    public void Cancel()
    {
        TransitionTo(State.Cancel());
        Raise(new OrderCancelledDomainEvent(Id));
    }

    private void TransitionTo(OrderState next) => Status = next.Status;

    private void EnsureLinesCanBeEdited()
    {
        if (!State.CanEditLines)
        {
            throw new DomainException($"Cannot change the lines of an order that is {Status}.");
        }
    }

    private OrderLine GetLine(Guid lineId) =>
        _lines.Find(line => line.Id == lineId)
        ?? throw new DomainException($"Order {Id} has no line {lineId}.");
}
