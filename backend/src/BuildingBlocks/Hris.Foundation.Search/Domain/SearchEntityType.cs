using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// A normalized searchable-entity-type name -- one of search-framework.md's own
/// Searchable Entities ("Employee", "Payroll", "Attendance Record", ...). Normalized to
/// uppercase, the identical reason <see cref="NumberPrefix"/>-style Value Objects in
/// this codebase normalize: without it, a caller passing "Employee" and another passing
/// "employee" would silently register two different <see cref="SearchIndexDefinition"/>
/// rows and split one entity type's own indexed content across both, a real correctness
/// bug this normalization prevents structurally rather than by caller discipline.
/// </summary>
public sealed partial class SearchEntityType : ValueObject
{
    private const int _maxLength = 100;

    public string Value { get; }

    private SearchEntityType(string value)
    {
        Value = value;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Not a lowercase transform -- ToUpperInvariant normalizes to " +
            "this type's own stated uppercase convention, the direction CA1308 does " +
            "not warn about; the rule's own name names the opposite transform.")]
    public static Result<SearchEntityType> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<SearchEntityType>(SearchErrors.EntityTypeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > _maxLength || !EntityTypePattern().IsMatch(normalized))
        {
            return Result.Failure<SearchEntityType>(SearchErrors.EntityTypeInvalid);
        }

        return Result.Success(new SearchEntityType(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z][A-Z0-9_]{0,99}$")]
    private static partial Regex EntityTypePattern();
}
