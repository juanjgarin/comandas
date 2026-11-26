using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.DeliverOrder;

public sealed class DeliverOrderCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : OrderCommandHandler<DeliverOrderCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, DeliverOrderCommand command) => order.Deliver();
}
