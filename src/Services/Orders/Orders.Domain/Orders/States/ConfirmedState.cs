namespace Comandas.Orders.Domain.Orders.States;

/// <summary>Sent to the kitchen but not started yet: it can still be cancelled.</summary>
internal sealed class ConfirmedState : OrderState
{
    public override OrderStatus Status => OrderStatus.Confirmed;

    public override OrderState StartPreparation() => InPreparation;

    public override OrderState Cancel() => Cancelled;
}
