using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : OrderCommandHandler<CancelOrderCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, CancelOrderCommand command) => order.Cancel();
}
