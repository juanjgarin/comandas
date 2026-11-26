namespace Comandas.Orders.Application.Orders.Commands.MarkOrderReady;

/// <summary>The kitchen finished the order (InPreparation -> Ready).</summary>
public sealed record MarkOrderReadyCommand(Guid OrderId) : IOrderCommand;
