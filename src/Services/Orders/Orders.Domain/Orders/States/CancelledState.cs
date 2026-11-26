namespace Comandas.Orders.Domain.Orders.States;

/// <summary>Final state: the order was cancelled before the kitchen started. Every action is rejected.</summary>
internal sealed class CancelledState : OrderState
{
    public override OrderStatus Status => OrderStatus.Cancelled;
}
