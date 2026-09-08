using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Bureau of Internal Revenue Tax Identification Number. Source:
/// docs/04-modules/employee/domain/employee-identification.md's Identifier
/// Catalogue ("TIN | Bureau of Internal Revenue tax identification | Yes"
/// format-validated). Validates a general digits-and-dashes shape (9-15
/// characters) rather than the BIR's full checksum rule, which would require
/// Statutory Reference Data this Sprint does not build -- a documented gap, not a
/// silently-invented rule, matching <see cref="PhoneNumber"/>'s own precedent.
/// Single-field, mapped via <c>HasConversion</c>.
/// </summary>
public sealed partial class Tin : ValueObject
{
    public string Value { get; }

    private Tin(string value)
    {
        Value = value;
    }

    public static Result<Tin> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Tin>(EmployeeErrors.TinInvalidFormat);
        }

        var normalized = value.Trim();
        return Pattern().IsMatch(normalized)
            ? Result.Success(new Tin(normalized))
            : Result.Failure<Tin>(EmployeeErrors.TinInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[0-9]{3}(-?[0-9]{3}){2}(-?[0-9]{3})?$")]
    private static partial Regex Pattern();
}
