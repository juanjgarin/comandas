using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders.Events;

/// <summary>The order was served at the table.</summary>
public sealed record OrderDeliveredDomainEvent(Guid OrderId) : IDomainEvent;
