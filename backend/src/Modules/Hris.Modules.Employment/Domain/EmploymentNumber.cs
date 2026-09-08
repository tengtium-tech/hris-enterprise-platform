using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The tenant-facing business identifier of an <see cref="Employment"/>, distinct
/// from its internal <see cref="EmploymentId"/> and never used as aggregate identity.
/// Source: docs/04-modules/employment/domain/employment-numbering.md's own "The Two
/// Identifiers Are Not Interchangeable" table. Caller-supplied and checked for
/// tenant-wide uniqueness at the repository, matching Position Number's own
/// established pattern -- this module does not take a compile-time reference to the
/// Numbering Framework project either.
/// </summary>
public sealed partial class EmploymentNumber : ValueObject
{
    private const int _maxLength = 50;

    public string Value { get; }

    private EmploymentNumber(string value)
    {
        Value = value;
    }

    public static Result<EmploymentNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<EmploymentNumber>(EmploymentErrors.EmploymentNumberRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<EmploymentNumber>(EmploymentErrors.EmploymentNumberTooLong)
            : Result.Success(new EmploymentNumber(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
