namespace Hris.SharedKernel;

/// <summary>
/// A marker Roslyn's own CA1062 ("validate parameter is non-null before using it")
/// recognizes by name: a public method that null-checks its own parameter and throws
/// when it fails is not, on its own, visible to CA1062's flow analysis as having
/// validated the *caller's* parameter -- attributing the guard method's own parameter
/// with a type named exactly <c>ValidatedNotNullAttribute</c> is the long-standing
/// convention (originating with FxCop, still honored by Roslyn's analyzers) that
/// closes that gap for a hand-written guard-clause helper such as <see cref="Guard"/>,
/// without which every public method in this solution that forwards a parameter
/// through <see cref="Guard.AgainstNull{T}"/> would need its own redundant null check
/// purely to satisfy the analyzer.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ValidatedNotNullAttribute : Attribute;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/guard-clauses.md: "A Guard
/// Clause immediately stops execution when a required condition is not satisfied."
/// Guards validate technical preconditions only (null, empty, out-of-range) and must
/// never encode a business rule (guard-clauses.md, "What Guard Clauses Do NOT
/// Validate": leave balance, promotion eligibility, and so on remain Aggregate
/// invariants -- see <c>invariants.md</c> and CTR-DAT-002).
///
/// Guards throw rather than return a <see cref="Result"/>, because a constructor
/// cannot return one (invariants.md, "Where Invariants Are Enforced": a Factory or
/// Value Object constructor must guarantee a valid object or not exist at all). Per
/// guard-clauses.md's own "Result Integration" section, a caller that wants an
/// expected-failure <see cref="Result"/> instead of a thrown exception -- typically a
/// factory validating a whole command's worth of input at once -- checks its inputs
/// explicitly before calling a guarded constructor, rather than catching the guard's
/// exception.
///
/// Flat static methods (<c>Guard.AgainstNull(...)</c>) rather than the common
/// <c>Guard.Against.Null(...)</c> nested-class idiom, per CA1034 ("do not nest
/// externally visible types") -- the nested-type shape is a naming convenience this
/// platform's own docs never mandate, so there is no reason to accept the analyzer
/// conflict for it.
/// </summary>
public static class Guard
{
    public static T AgainstNull<T>([ValidatedNotNull] T? value, string paramName)
        where T : class
    {
        return value ?? throw new ArgumentNullException(paramName);
    }

    public static string AgainstNullOrEmpty([ValidatedNotNull] string? value, string paramName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty.", paramName);
        }

        return value;
    }

    public static string AgainstNullOrWhiteSpace([ValidatedNotNull] string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }

        return value;
    }

    public static Guid AgainstDefault(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be an empty Guid.", paramName);
        }

        return value;
    }

    public static decimal AgainstNegative(decimal value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
        }

        return value;
    }

    public static decimal AgainstNegativeOrZero(decimal value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be greater than zero.");
        }

        return value;
    }

    public static DateOnly AgainstOutOfRange(DateOnly value, DateOnly rangeStart, DateOnly rangeEnd, string paramName)
    {
        if (value < rangeStart || value > rangeEnd)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Value must fall between {rangeStart:O} and {rangeEnd:O}.");
        }

        return value;
    }
}
