using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders.Events;

/// <summary>The kitchen started cooking the order.</summary>
public sealed record OrderPreparationStartedDomainEvent(Guid OrderId) : IDomainEvent;
