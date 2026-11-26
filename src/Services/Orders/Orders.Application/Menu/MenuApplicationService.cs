using Comandas.Orders.Application.Abstractions;

namespace Comandas.Orders.Application.Menu;

/// <summary>Use cases for reading the menu.</summary>
public sealed class MenuApplicationService(IMenuItemRepository menuItems)
{
    public async Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(CancellationToken cancellationToken)
    {
        var items = await menuItems.GetAllAsync(cancellationToken);

        return
        [
            .. items
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(item =>
                {
                    var price = item.GetPrice(); // polymorphic: dish or combo, the service does not care
                    return new MenuItemDto(
                        item.Id,
                        item.Name,
                        item.GetType().Name,
                        price.Amount,
                        price.Currency,
                        item.CanBeOrdered,
                        item.IsTwoForOneEligible);
                }),
        ];
    }
}
