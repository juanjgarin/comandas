using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.ConfirmOrder;

public sealed class ConfirmOrderCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : OrderCommandHandler<ConfirmOrderCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, ConfirmOrderCommand command) => order.Confirm();
}
