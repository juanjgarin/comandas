namespace Comandas.Orders.Domain.Orders.States;

/// <summary>The waiter is still building the order: lines can change; it can be confirmed or cancelled.</summary>
internal sealed class CreatedState : OrderState
{
    public override OrderStatus Status => OrderStatus.Created;

    public override bool CanEditLines => true;

    public override OrderState Confirm() => Confirmed;

    public override OrderState Cancel() => Cancelled;
}
