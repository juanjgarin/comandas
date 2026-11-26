using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.RemoveOrderLine;

public sealed class RemoveOrderLineCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : OrderCommandHandler<RemoveOrderLineCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, RemoveOrderLineCommand command) => order.RemoveLine(command.LineId);
}
