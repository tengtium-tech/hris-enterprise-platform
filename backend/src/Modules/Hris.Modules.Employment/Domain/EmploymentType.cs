using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The legal and contractual classification of an Employment -- Regular,
/// Probationary, Contractual, and so on. Source:
/// docs/04-modules/employment/domain/employment-types.md's own Type Catalogue and
/// its "Organizations may configure additional types" note. Modeled as a
/// tenant-configurable string Value Object rather than a closed enum, matching
/// Position Type's own established precedent, since the base catalogue is
/// explicitly extensible (statutory types may not be removed, but this module has
/// no statutory-type catalog to enforce that against yet -- a documented gap, not a
/// silently dropped rule).
/// </summary>
public sealed partial class EmploymentType : ValueObject
{
    private const int _maxLength = 100;

    public string Value { get; }

    private EmploymentType(string value)
    {
        Value = value;
    }

    public static Result<EmploymentType> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<EmploymentType>(EmploymentErrors.EmploymentTypeRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<EmploymentType>(EmploymentErrors.EmploymentTypeTooLong)
            : Result.Success(new EmploymentType(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
