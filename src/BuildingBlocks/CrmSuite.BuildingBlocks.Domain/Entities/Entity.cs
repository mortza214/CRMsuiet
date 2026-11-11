namespace CrmSuite.BuildingBlocks.Domain.Entities;

/// <summary>
/// Base entity keyed by <typeparamref name="TId"/>.
/// Equality is based on identity and concrete type, never on mutable state.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public bool Equals(Entity<TId>? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || GetType() != other.GetType())
        {
            return false;
        }

        // Two not-yet-persisted entities must not be treated as the same record.
        if (EqualityComparer<TId>.Default.Equals(Id, default!))
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}

/// <summary>Convenience base entity for aggregates and tables whose key is a <see cref="Guid"/>.</summary>
public abstract class Entity : Entity<Guid>
{
}
