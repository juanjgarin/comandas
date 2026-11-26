using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.StartOrderPreparation;

public sealed class StartOrderPreparationCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : OrderCommandHandler<StartOrderPreparationCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, StartOrderPreparationCommand command) => order.StartPreparation();
}
