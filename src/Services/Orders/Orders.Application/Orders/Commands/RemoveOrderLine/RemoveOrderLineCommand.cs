namespace Comandas.Orders.Application.Orders.Commands.RemoveOrderLine;

/// <summary>Removes one line from the order.</summary>
public sealed record RemoveOrderLineCommand(Guid OrderId, Guid LineId) : IOrderCommand;
