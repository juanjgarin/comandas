using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.Orders.Domain.Orders.Events;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Orders;

/// <summary>
/// The order announces what happened to it through domain events (Observer).
/// Here we only check that the right events are collected; dispatching them comes in phase 2.
/// </summary>
public sealed class OrderDomainEventsTests
{
    private const int Table = 7;

    [Fact]
    public void Create_RaisesOrderCreated()
    {
        var order = Order.Create(Table, "EUR");

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new OrderCreatedDomainEvent(order.Id, Table));
    }

    [Fact]
    public void Confirm_RaisesOrderConfirmed()
    {
        var order = NewOrderWithOneLine();

        order.Confirm();

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new OrderConfirmedDomainEvent(order.Id, Table));
    }

    [Fact]
    public void StartPreparation_RaisesOrderPreparationStarted()
    {
        var order = NewOrderWithOneLine();
        order.Confirm();
        order.ClearDomainEvents();

        order.StartPreparation();

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new OrderPreparationStartedDomainEvent(order.Id));
    }

    [Fact]
    public void MarkReady_RaisesOrderReady()
    {
        var order = NewOrderWithOneLine();
        order.Confirm();
        order.StartPreparation();
        order.ClearDomainEvents();

        order.MarkReady();

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new OrderReadyDomainEvent(order.Id, Table));
    }

    [Fact]
    public void Deliver_RaisesOrderDelivered()
    {
        var order = NewOrderWithOneLine();
        order.Confirm();
        order.StartPreparation();
        order.MarkReady();
        order.ClearDomainEvents();

        order.Deliver();

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new OrderDeliveredDomainEvent(order.Id));
    }

    [Fact]
    public void Cancel_RaisesOrderCancelled()
    {
        var order = NewOrderWithOneLine();

        order.Cancel();

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new OrderCancelledDomainEvent(order.Id));
    }

    [Fact]
    public void FullLifecycle_RaisesEventsInOrder()
    {
        var order = Order.Create(Table, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);

        order.Confirm();
        order.StartPreparation();
        order.MarkReady();
        order.Deliver();

        order.DomainEvents.Select(domainEvent => domainEvent.GetType()).Should().Equal(
            typeof(OrderCreatedDomainEvent),
            typeof(OrderConfirmedDomainEvent),
            typeof(OrderPreparationStartedDomainEvent),
            typeof(OrderReadyDomainEvent),
            typeof(OrderDeliveredDomainEvent));
    }

    [Fact]
    public void RejectedTransition_RaisesNoEvent()
    {
        var order = NewOrderWithOneLine();

        var act = order.Deliver; // Created -> Delivered is not allowed

        act.Should().Throw<DomainException>();
        order.DomainEvents.Should().BeEmpty("nothing happened, so nothing is announced");
    }

    [Fact]
    public void ConfirmEmptyOrder_RaisesNoEvent()
    {
        var order = Order.Create(Table, "EUR");
        order.ClearDomainEvents();

        var act = order.Confirm;

        act.Should().Throw<DomainException>();
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void LineChanges_RaiseNoEvents()
    {
        // Editing a draft order is internal to the waiter's work: nobody outside needs to react to it yet.
        var order = Order.Create(Table, "EUR");
        order.ClearDomainEvents();

        var line = order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);
        order.ChangeLineQuantity(line.Id, Quantity.Of(2));
        order.RemoveLine(line.Id);

        order.DomainEvents.Should().BeEmpty();
    }

    /// <summary>An order ready to be confirmed, with its OrderCreated event already cleared.</summary>
    private static Order NewOrderWithOneLine()
    {
        var order = Order.Create(Table, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);
        order.ClearDomainEvents();
        return order;
    }
}
