namespace Comandas.Orders.Domain.Orders.States;

/// <summary>Final state: served at the table. No transition overridden, so every action is rejected.</summary>
internal sealed class DeliveredState : OrderState
{
    public override OrderStatus Status => OrderStatus.Delivered;
}
