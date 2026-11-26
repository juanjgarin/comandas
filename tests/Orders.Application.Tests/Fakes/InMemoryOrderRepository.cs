using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Tests.Fakes;

/// <summary>Test double: a real (tiny) implementation of the repository contract, backed by a dictionary.</summary>
internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public IReadOnlyCollection<Order> All => _orders.Values;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.GetValueOrDefault(id));

    public void Add(Order order) => _orders.Add(order.Id, order);
}
