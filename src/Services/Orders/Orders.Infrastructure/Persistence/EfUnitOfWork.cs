using Comandas.Orders.Application.Abstractions;

namespace Comandas.Orders.Infrastructure.Persistence;

/// <summary>
/// Unit of Work over EF Core. The DbContext already tracks every change made through the repositories
/// during a request (they all share the same scoped DbContext); SaveChanges sends them in ONE transaction.
/// Domain event dispatching will hook in here in step 2.10.
/// </summary>
internal sealed class EfUnitOfWork(OrdersDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
