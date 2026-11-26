using Comandas.Orders.Domain.Orders;

namespace Comandas.Orders.Application.Orders;

/// <summary>Domain -> DTO mapping, written by hand: explicit, debuggable and with no mapping library.</summary>
internal static class OrderMappings
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.TableNumber,
        order.Status.ToString(),
        order.Currency,
        order.Subtotal.Amount,
        order.Discount?.Description,
        order.Discount?.Amount.Amount ?? 0m,
        order.Total.Amount,
        [.. order.Lines.Select(line => new OrderLineDto(
            line.Id,
            line.MenuItemId,
            line.ItemName,
            line.UnitPrice.Amount,
            line.Quantity.Value,
            line.LineTotal.Amount))]);
}
