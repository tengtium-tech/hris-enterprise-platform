namespace Hris.SharedKernel;

/// <summary>
/// Base type for every Entity in the platform: a business object distinguished by
/// identity rather than by its current attribute values.
///
/// Grounded in docs/02-architecture/04-domain-driven-design/entities.md's Entity
/// Characteristics ("unique identity ... equality based on identity") and Entity
/// Equality section ("Even if Name changes ... The Entity remains the same
/// Employee"). Equality is therefore implemented once here, by <typeparamref name="TId"/>
/// alone, so no derived Entity can accidentally fall back to field-by-field equality
/// (entities.md's own "Common Anti-Patterns": "Equality based on mutable fields").
///
/// A concrete Entity derives from this and exposes behavior methods, never public
/// setters, per entities.md's Rich Domain Model section -- state changes only
/// through methods such as <c>employment.Promote(newPosition)</c>.
/// </summary>
public abstract class Entity<TId>
    where TId : IStronglyTypedId
{
    public TId Id { get; }

    protected Entity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
