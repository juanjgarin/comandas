namespace Comandas.Orders.Domain.Orders.States;

/// <summary>Waiting at the pass for the waiter to take it to the table.</summary>
internal sealed class ReadyState : OrderState
{
    public override OrderStatus Status => OrderStatus.Ready;

    public override OrderState Deliver() => Delivered;
}
