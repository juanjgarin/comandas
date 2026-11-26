using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Application.Common;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders;

/// <summary>
/// Orchestrates the order use cases: load the aggregate, ask the DOMAIN to do the work, save through the
/// Unit of Work. No business rules live here. It knows only abstractions, never EF Core (Dependency Inversion).
/// </summary>
/// <remarks>
/// Deliberately one class with many use cases, being split into one CQRS handler per use case
/// (step 2.6 moved "create order" to <c>CreateOrderCommandHandler</c>; steps 2.7–2.8 move the rest and delete
/// this class), to see the Single Responsibility Principle at work on real code.
/// </remarks>
public sealed class OrderApplicationService(
    IOrderRepository orders,
    IMenuItemRepository menuItems,
    IUnitOfWork unitOfWork)
{
    public async Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await GetExistingOrderAsync(orderId, cancellationToken);
        return order.ToDto();
    }

    /// <returns>The id of the line that now contains the item (new or merged).</returns>
    public async Task<Guid> AddItemAsync(Guid orderId, Guid menuItemId, int quantity, CancellationToken cancellationToken)
    {
        var order = await GetExistingOrderAsync(orderId, cancellationToken);
        var menuItem = await menuItems.GetByIdAsync(menuItemId, cancellationToken)
            ?? throw NotFoundException.For("Menu item", menuItemId);

        var line = order.AddItem(menuItem, Quantity.Of(quantity));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return line.Id;
    }

    public Task ChangeLineQuantityAsync(Guid orderId, Guid lineId, int quantity, CancellationToken cancellationToken) =>
        ExecuteAsync(orderId, order => order.ChangeLineQuantity(lineId, Quantity.Of(quantity)), cancellationToken);

    public Task RemoveLineAsync(Guid orderId, Guid lineId, CancellationToken cancellationToken) =>
        ExecuteAsync(orderId, order => order.RemoveLine(lineId), cancellationToken);

    public Task ConfirmAsync(Guid orderId, CancellationToken cancellationToken) =>
        ExecuteAsync(orderId, order => order.Confirm(), cancellationToken);

    public Task StartPreparationAsync(Guid orderId, CancellationToken cancellationToken) =>
        ExecuteAsync(orderId, order => order.StartPreparation(), cancellationToken);

    public Task MarkReadyAsync(Guid orderId, CancellationToken cancellationToken) =>
        ExecuteAsync(orderId, order => order.MarkReady(), cancellationToken);

    public Task DeliverAsync(Guid orderId, CancellationToken cancellationToken) =>
        ExecuteAsync(orderId, order => order.Deliver(), cancellationToken);

    public Task CancelAsync(Guid orderId, CancellationToken cancellationToken) =>
        ExecuteAsync(orderId, order => order.Cancel(), cancellationToken);

    /// <summary>The common script of every command: load, let the domain act, save.</summary>
    private async Task ExecuteAsync(Guid orderId, Action<Order> domainOperation, CancellationToken cancellationToken)
    {
        var order = await GetExistingOrderAsync(orderId, cancellationToken);

        domainOperation(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Order> GetExistingOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await orders.GetByIdAsync(orderId, cancellationToken)
        ?? throw NotFoundException.For("Order", orderId);
}
