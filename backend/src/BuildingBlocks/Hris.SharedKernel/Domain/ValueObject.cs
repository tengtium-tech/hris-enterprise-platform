namespace Hris.SharedKernel;

/// <summary>
/// Base type for every Value Object in the platform: an immutable business concept
/// with no identity, compared by its components rather than by reference.
///
/// Grounded in docs/02-architecture/04-domain-driven-design/value-objects.md's Design
/// Principles ("immutable ... self-validating ... compared by value") and Equality
/// section ("Money(1000, PHP) == Money(1000, PHP) ... Identity is irrelevant").
/// <see cref="GetEqualityComponents"/> is the single place a derived Value Object
/// states what it is equal by; <see cref="Equals(object?)"/> and
/// <see cref="GetHashCode"/> are implemented once here so no derived type can drift
/// into reference equality or a partial field comparison.
///
/// A derived Value Object still validates its own inputs in its constructor via
/// <see cref="Guard"/> (guard-clauses.md, "Value Objects should use Guard Clauses to
/// validate constructor parameters") so that, per value-objects.md, "Invalid Value
/// Objects should never exist" -- this base type only supplies equality, not
/// validation, since validation rules are specific to each concept (email format,
/// currency required, and so on).
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) => Equals((object?)other);

    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject other || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
