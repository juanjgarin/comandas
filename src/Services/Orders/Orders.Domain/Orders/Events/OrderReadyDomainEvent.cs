using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders.Events;

/// <summary>The order is ready at the pass (phase 5: triggers the customer notification).</summary>
public sealed record OrderReadyDomainEvent(Guid OrderId, int TableNumber) : IDomainEvent;
