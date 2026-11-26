using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Menu;

namespace Comandas.Orders.Application.Tests.Fakes;

internal sealed class InMemoryMenuItemRepository(params MenuItem[] items) : IMenuItemRepository
{
    private readonly List<MenuItem> _items = [.. items];

    public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_items.Find(item => item.Id == id));

    public Task<IReadOnlyList<MenuItem>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MenuItem>>(_items);
}
