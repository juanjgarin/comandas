using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Application.Common;
using Comandas.Orders.Domain.Orders;
using MediatR;

namespace Comandas.Orders.Application.Orders.Commands.AddOrderItem;

/// <summary>
/// Not built on <see cref="OrderCommandHandler{TCommand}"/>: it returns a value and needs a second repository
/// (the menu), so forcing it into the template would make the template worse for every other command.
/// </summary>
public sealed class AddOrderItemCommandHandler(
    IOrderRepository orders,
    IMenuItemRepository menuItems,
    IUnitOfWork unitOfWork) : IRequestHandler<AddOrderItemCommand, Guid>
{
    public async Task<Guid> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await orders.GetExistingAsync(request.OrderId, cancellationToken);
        var menuItem = await menuItems.GetByIdAsync(request.MenuItemId, cancellationToken)
            ?? throw NotFoundException.For("Menu item", request.MenuItemId);

        var line = order.AddItem(menuItem, Quantity.Of(request.Quantity));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return line.Id;
    }
}
