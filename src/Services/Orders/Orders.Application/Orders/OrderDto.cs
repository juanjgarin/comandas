namespace Comandas.Orders.Application.Orders;

/// <summary>Read model of an order returned to callers. Plain data: callers cannot change the domain through it.</summary>
public sealed record OrderDto(
    Guid Id,
    int TableNumber,
    string Status,
    string Currency,
    decimal Subtotal,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record OrderLineDto(
    Guid Id,
    Guid MenuItemId,
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
