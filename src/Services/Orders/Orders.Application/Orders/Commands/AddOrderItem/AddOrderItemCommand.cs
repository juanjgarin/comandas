using MediatR;

namespace Comandas.Orders.Application.Orders.Commands.AddOrderItem;

/// <summary>Adds a menu item to the order. Returns the id of the line that now holds it (new or merged).</summary>
public sealed record AddOrderItemCommand(Guid OrderId, Guid MenuItemId, int Quantity) : IRequest<Guid>;
