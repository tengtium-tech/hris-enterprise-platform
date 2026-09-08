using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Philippine Health Insurance Corporation number. Source:
/// docs/04-modules/employee/domain/employee-identification.md's Identifier
/// Catalogue. See <see cref="Tin"/>'s own remarks on the format-validation
/// boundary this Sprint draws.
/// </summary>
public sealed partial class PhilHealthNumber : ValueObject
{
    public string Value { get; }

    private PhilHealthNumber(string value)
    {
        Value = value;
    }

    public static Result<PhilHealthNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PhilHealthNumber>(EmployeeErrors.PhilHealthNumberInvalidFormat);
        }

        var normalized = value.Trim();
        return Pattern().IsMatch(normalized)
            ? Result.Success(new PhilHealthNumber(normalized))
            : Result.Failure<PhilHealthNumber>(EmployeeErrors.PhilHealthNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[0-9]{2}-?[0-9]{9}-?[0-9]{1}$")]
    private static partial Regex Pattern();
}
