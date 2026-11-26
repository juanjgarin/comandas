using MediatR;

namespace Comandas.Orders.Application.Orders.Commands.CreateOrder;

/// <summary>
/// CQRS command: the intention "open an order for this table", as an immutable message.
/// It returns only the new order's id (commands change state; queries return data).
/// </summary>
public sealed record CreateOrderCommand(int TableNumber) : IRequest<Guid>;
