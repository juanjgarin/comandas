namespace Comandas.Orders.Application.Abstractions;

/// <summary>
/// Commits every change made through the repositories during one use case, atomically (Unit of Work).
/// One use case = one call to <see cref="SaveChangesAsync"/>.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
