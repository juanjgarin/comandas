using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders.Events;

/// <summary>A waiter opened a new order for a table.</summary>
public sealed record OrderCreatedDomainEvent(Guid OrderId, int TableNumber) : IDomainEvent;
