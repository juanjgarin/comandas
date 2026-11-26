using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Discounts;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders.Commands.ApplyOrderDiscount;

/// <summary>
/// Joins the pieces of steps 2.4 and 2.5: the Factory chooses the strategy, the aggregate applies it and keeps
/// the snapshot. The handler only coordinates; it decides nothing about discounts itself.
/// </summary>
public sealed class ApplyOrderDiscountCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IDiscountFactory discountFactory)
    : OrderCommandHandler<ApplyOrderDiscountCommand>(orders, unitOfWork)
{
    protected override void Execute(Order order, ApplyOrderDiscountCommand command)
    {
        var strategy = discountFactory.CreateBestFor(
            DiscountContext.For(order), new DiscountRequest(command.ManualPercentage));

        order.ApplyDiscount(strategy);
    }
}
