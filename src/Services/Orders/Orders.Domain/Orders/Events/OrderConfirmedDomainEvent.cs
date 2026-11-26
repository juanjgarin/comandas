using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders.Events;

/// <summary>The order was sent to the kitchen (phase 4: becomes the OrderConfirmed integration event).</summary>
public sealed record OrderConfirmedDomainEvent(Guid OrderId, int TableNumber) : IDomainEvent;
