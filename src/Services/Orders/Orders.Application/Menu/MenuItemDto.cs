namespace Comandas.Orders.Application.Menu;

/// <summary>A menu entry as shown to the waiter. <see cref="Kind"/> is "Dish" or "Combo".</summary>
public sealed record MenuItemDto(
    Guid Id,
    string Name,
    string Kind,
    decimal Price,
    string Currency,
    bool CanBeOrdered,
    bool IsTwoForOneEligible);
