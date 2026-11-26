namespace Comandas.SharedKernel.Domain;

/// <summary>
/// An object defined by its identity, not by its attributes: two orders with the same lines
/// are still different orders. Equality is therefore based on <see cref="Id"/> (and type).
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    // Parameterless constructor for EF Core materialization only.
    protected Entity()
    {
        Id = default!; // Set by EF Core when loading from the database.
    }

    public TId Id { get; private set; }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
