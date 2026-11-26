using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.MarkOrderReady;

public sealed class MarkOrderReadyCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : OrderCommandHandler<MarkOrderReadyCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, MarkOrderReadyCommand command) => order.MarkReady();
}
