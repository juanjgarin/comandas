namespace Comandas.Orders.Domain.Orders.States;

/// <summary>The kitchen is cooking: too late to cancel.</summary>
internal sealed class InPreparationState : OrderState
{
    public override OrderStatus Status => OrderStatus.InPreparation;

    public override OrderState MarkReady() => Ready;
}
