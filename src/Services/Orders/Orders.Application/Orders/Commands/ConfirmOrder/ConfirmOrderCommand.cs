namespace Comandas.Orders.Application.Orders.Commands.ConfirmOrder;

/// <summary>The order is complete and goes to the kitchen (Created -> Confirmed).</summary>
public sealed record ConfirmOrderCommand(Guid OrderId) : IOrderCommand;
