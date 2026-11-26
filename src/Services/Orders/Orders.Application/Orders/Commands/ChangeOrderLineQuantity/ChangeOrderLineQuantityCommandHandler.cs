using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.ChangeOrderLineQuantity;

public sealed class ChangeOrderLineQuantityCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : OrderCommandHandler<ChangeOrderLineQuantityCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, ChangeOrderLineQuantityCommand command) =>
        order.ChangeLineQuantity(command.LineId, Quantity.Of(command.Quantity));
}
