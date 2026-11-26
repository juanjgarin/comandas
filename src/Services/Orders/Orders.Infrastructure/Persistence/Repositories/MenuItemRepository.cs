using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Menu;
using Microsoft.EntityFrameworkCore;

namespace Comandas.Orders.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMenuItemRepository"/>. The menu is only READ in this service,
/// so it is loaded without change tracking.
/// </summary>
/// <remarks>
/// Combos can be nested to any depth, and <c>Include</c> is not recursive. Instead, the whole menu
/// (a few dozen rows) is loaded in ONE query with every combo's components, using identity resolution:
/// each row becomes exactly one object, so a nested combo is the same instance that already has its own
/// components, and the Composite tree is complete at every level.
/// </remarks>
internal sealed class MenuItemRepository(OrdersDbContext context) : IMenuItemRepository
{
    public async Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var menu = await LoadWholeMenuAsync(cancellationToken);
        return menu.FirstOrDefault(item => item.Id == id);
    }

    public Task<IReadOnlyList<MenuItem>> GetAllAsync(CancellationToken cancellationToken) =>
        LoadWholeMenuAsync(cancellationToken);

    private async Task<IReadOnlyList<MenuItem>> LoadWholeMenuAsync(CancellationToken cancellationToken) =>
        await context.MenuItems
            .AsNoTrackingWithIdentityResolution()
            .Include(item => ((Combo)item).Components) // include on a derived type (TPH): only combo rows have components
                .ThenInclude(component => component.Item)
            .AsSplitQuery() // one SQL query per collection instead of one wide JOIN
            .ToListAsync(cancellationToken);
}
