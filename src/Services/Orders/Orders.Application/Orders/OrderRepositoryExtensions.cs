using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Application.Common;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders;

internal static class OrderRepositoryExtensions
{
    /// <summary>Loads the order or throws <see cref="NotFoundException"/>: the same "not found" rule for every use case.</summary>
    public static async Task<Order> GetExistingAsync(
        this IOrderRepository orders, Guid orderId, CancellationToken cancellationToken) =>
        await orders.GetByIdAsync(orderId, cancellationToken)
        ?? throw NotFoundException.For("Order", orderId);
}
