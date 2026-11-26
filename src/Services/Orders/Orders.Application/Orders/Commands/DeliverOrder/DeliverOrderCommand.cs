namespace Comandas.Orders.Application.Orders.Commands.DeliverOrder;

/// <summary>The order was served at the table (Ready -> Delivered).</summary>
public sealed record DeliverOrderCommand(Guid OrderId) : IOrderCommand;
