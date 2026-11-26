namespace Comandas.Orders.Application.Orders.Commands.ChangeOrderLineQuantity;

/// <summary>Sets a new quantity on one line of the order.</summary>
public sealed record ChangeOrderLineQuantityCommand(Guid OrderId, Guid LineId, int Quantity) : IOrderCommand;
