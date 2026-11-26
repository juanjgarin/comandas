namespace Comandas.Orders.Application.Orders.Commands.ApplyOrderDiscount;

/// <summary>
/// Applies the best discount available right now (decision D6). <paramref name="ManualPercentage"/> is an
/// optional percentage the waiter asks for (e.g. staff discount); it competes with the automatic ones.
/// </summary>
public sealed record ApplyOrderDiscountCommand(Guid OrderId, decimal? ManualPercentage = null) : IOrderCommand;
