using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Comandas.Orders.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>. Orders are loaded TRACKED because callers
/// load them to change them: EF records every change and the Unit of Work saves them together.
/// </summary>
internal sealed class OrderRepository(OrdersDbContext context) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Orders
            .Include(order => order.Lines) // an aggregate is always loaded whole: root + lines
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public void Add(Order order) => context.Orders.Add(order);
}
