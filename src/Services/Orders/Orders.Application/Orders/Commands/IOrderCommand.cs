using MediatR;

namespace Comandas.Orders.Application.Orders.Commands;

/// <summary>A command that acts on one existing order and returns nothing.</summary>
public interface IOrderCommand : IRequest
{
    Guid OrderId { get; }
}
