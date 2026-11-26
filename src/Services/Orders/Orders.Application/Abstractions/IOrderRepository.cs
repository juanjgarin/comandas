using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Abstractions;

/// <summary>
/// Collection-like access to Order aggregates (Repository pattern). Declared here, implemented in
/// Infrastructure (Dependency Inversion). It does NOT save: committing is the Unit of Work's job (ISP).
/// </summary>
public interface IOrderRepository
{
    /// <summary>Loads the whole aggregate (order + lines), tracked for changes. Null if it does not exist.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Order order);
}
