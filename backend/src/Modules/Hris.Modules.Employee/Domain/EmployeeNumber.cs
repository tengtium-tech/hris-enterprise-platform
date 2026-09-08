using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// The tenant-facing business identifier of an <see cref="Employee"/>, distinct
/// from its internal <see cref="EmployeeId"/> and never used as aggregate identity.
/// Source: docs/04-modules/employee/domain/employee-numbering.md's own "Identifier
/// Types" table. Caller-supplied and checked for tenant-wide uniqueness at the
/// repository, matching Employment Number's own established pattern -- this module
/// does not take a compile-time reference to the Numbering Framework project
/// either.
/// </summary>
public sealed partial class EmployeeNumber : ValueObject
{
    private const int _maxLength = 50;

    public string Value { get; }

    private EmployeeNumber(string value)
    {
        Value = value;
    }

    public static Result<EmployeeNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<EmployeeNumber>(EmployeeErrors.EmployeeNumberRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<EmployeeNumber>(EmployeeErrors.EmployeeNumberTooLong)
            : Result.Success(new EmployeeNumber(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
