namespace Comandas.SharedKernel.Domain;

/// <summary>
/// Marker for something meaningful that happened inside the domain (e.g. "order confirmed").
/// Aggregates raise them; handlers react to them later (Observer pattern).
/// </summary>
public interface IDomainEvent;
