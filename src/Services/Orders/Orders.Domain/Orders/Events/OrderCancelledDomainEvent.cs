using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders.Events;

/// <summary>The order was cancelled before the kitchen started it.</summary>
public sealed record OrderCancelledDomainEvent(Guid OrderId) : IDomainEvent;
