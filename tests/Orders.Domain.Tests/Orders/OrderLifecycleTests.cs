using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Orders;

/// <summary>
/// Exhaustive tests of the order lifecycle (State pattern): every status × every action.
/// </summary>
public sealed class OrderLifecycleTests
{
    public enum OrderAction
    {
        Confirm,
        StartPreparation,
        MarkReady,
        Deliver,
        Cancel,
    }

    /// <summary>The only valid transitions, straight from the state diagram in PLAN.md.</summary>
    public static readonly TheoryData<OrderStatus, OrderAction, OrderStatus> ValidTransitions = new()
    {
        { OrderStatus.Created, OrderAction.Confirm, OrderStatus.Confirmed },
        { OrderStatus.Created, OrderAction.Cancel, OrderStatus.Cancelled },
        { OrderStatus.Confirmed, OrderAction.StartPreparation, OrderStatus.InPreparation },
        { OrderStatus.Confirmed, OrderAction.Cancel, OrderStatus.Cancelled },
        { OrderStatus.InPreparation, OrderAction.MarkReady, OrderStatus.Ready },
        { OrderStatus.Ready, OrderAction.Deliver, OrderStatus.Delivered },
    };

    /// <summary>Every (status, action) pair that is NOT in <see cref="ValidTransitions"/>: 30 - 6 = 24 cases.</summary>
    public static TheoryData<OrderStatus, OrderAction> InvalidTransitions()
    {
        var valid = ValidTransitions
            .Select(row => ((OrderStatus)row.Data.Item1, (OrderAction)row.Data.Item2))
            .ToHashSet();

        var data = new TheoryData<OrderStatus, OrderAction>();
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            foreach (var action in Enum.GetValues<OrderAction>())
            {
                if (!valid.Contains((status, action)))
                {
                    data.Add(status, action);
                }
            }
        }

        return data;
    }

    [Fact]
    public void Create_StartsInCreatedStatus()
    {
        Order.Create(1, "EUR").Status.Should().Be(OrderStatus.Created);
    }

    [Theory]
    [MemberData(nameof(ValidTransitions))]
    public void Transition_Allowed_MovesToNextStatus(OrderStatus from, OrderAction action, OrderStatus expected)
    {
        var order = OrderIn(from);

        Apply(order, action);

        order.Status.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void Transition_NotAllowed_ThrowsDomainExceptionAndKeepsStatus(OrderStatus from, OrderAction action)
    {
        var order = OrderIn(from);

        var act = () => Apply(order, action);

        act.Should().Throw<DomainException>().WithMessage($"Cannot * an order that is {from}.");
        order.Status.Should().Be(from);
    }

    [Fact]
    public void InvalidTransitions_CoverAllRemainingCombinations()
    {
        var all = Enum.GetValues<OrderStatus>().Length * Enum.GetValues<OrderAction>().Length;

        InvalidTransitions().Should().HaveCount(all - ValidTransitions.Count);
    }

    [Fact]
    public void Confirm_EmptyOrder_ThrowsDomainException()
    {
        var order = Order.Create(1, "EUR");

        var act = order.Confirm;

        act.Should().Throw<DomainException>().WithMessage("*without lines*");
        order.Status.Should().Be(OrderStatus.Created);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.InPreparation)]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void AddItem_WhenNotCreated_ThrowsDomainException(OrderStatus status)
    {
        var order = OrderIn(status);

        var act = () => order.AddItem(NewBurger(), Quantity.One);

        act.Should().Throw<DomainException>().WithMessage($"Cannot change the lines of an order that is {status}.");
    }

    [Fact]
    public void ChangeLineQuantity_AfterConfirm_ThrowsDomainException()
    {
        var order = OrderIn(OrderStatus.Confirmed);
        var lineId = order.Lines.Single().Id;

        var act = () => order.ChangeLineQuantity(lineId, Quantity.Of(5));

        act.Should().Throw<DomainException>();
        order.Lines.Single().Quantity.Should().Be(Quantity.One);
    }

    [Fact]
    public void RemoveLine_AfterConfirm_ThrowsDomainException()
    {
        var order = OrderIn(OrderStatus.Confirmed);
        var lineId = order.Lines.Single().Id;

        var act = () => order.RemoveLine(lineId);

        act.Should().Throw<DomainException>();
        order.Lines.Should().ContainSingle();
    }

    /// <summary>Builds an order with one line and walks it through valid transitions up to <paramref name="status"/>.</summary>
    private static Order OrderIn(OrderStatus status)
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(NewBurger(), Quantity.One);

        OrderAction[] path = status switch
        {
            OrderStatus.Created => [],
            OrderStatus.Confirmed => [OrderAction.Confirm],
            OrderStatus.InPreparation => [OrderAction.Confirm, OrderAction.StartPreparation],
            OrderStatus.Ready => [OrderAction.Confirm, OrderAction.StartPreparation, OrderAction.MarkReady],
            OrderStatus.Delivered => [OrderAction.Confirm, OrderAction.StartPreparation, OrderAction.MarkReady, OrderAction.Deliver],
            OrderStatus.Cancelled => [OrderAction.Cancel],
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        foreach (var action in path)
        {
            Apply(order, action);
        }

        order.Status.Should().Be(status, "the test helper must reach the requested status");
        return order;
    }

    private static void Apply(Order order, OrderAction action)
    {
        switch (action)
        {
            case OrderAction.Confirm: order.Confirm(); break;
            case OrderAction.StartPreparation: order.StartPreparation(); break;
            case OrderAction.MarkReady: order.MarkReady(); break;
            case OrderAction.Deliver: order.Deliver(); break;
            case OrderAction.Cancel: order.Cancel(); break;
            default: throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    private static Dish NewBurger() => Dish.Create("Burger", Money.Of(10m, "EUR"));
}
