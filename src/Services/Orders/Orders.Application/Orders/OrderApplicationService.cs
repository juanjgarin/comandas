using Comandas.Orders.Application.Abstractions;

namespace Comandas.Orders.Application.Orders;

/// <summary>
/// What is left of the original "fat" application service: every write use case is now a CQRS command with
/// its own handler (steps 2.6–2.7). The remaining read becomes a query in step 2.8, and this class goes away.
/// </summary>
public sealed class OrderApplicationService(IOrderRepository orders)
{
    public async Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orders.GetExistingAsync(orderId, cancellationToken);
        return order.ToDto();
    }
}
