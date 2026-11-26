using Comandas.Orders.Domain.Menu;

namespace Comandas.Orders.Application.Abstractions;

/// <summary>Read access to the menu (dishes and combos, with their components loaded).</summary>
public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuItem>> GetAllAsync(CancellationToken cancellationToken);
}
