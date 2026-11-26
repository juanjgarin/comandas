namespace Comandas.Orders.Application.Orders.Commands.CancelOrder;

/// <summary>The order is cancelled (only allowed from Created or Confirmed).</summary>
public sealed record CancelOrderCommand(Guid OrderId) : IOrderCommand;
