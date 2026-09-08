using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Government Service Insurance System number (public-sector employees). Source:
/// docs/04-modules/employee/domain/employee-identification.md's Identifier
/// Catalogue. See <see cref="SssNumber"/>'s own remarks on the sector-based
/// mutual-exclusivity this module does not enforce, and <see cref="Tin"/>'s own
/// remarks on the format-validation boundary this Sprint draws.
/// </summary>
public sealed partial class GsisNumber : ValueObject
{
    public string Value { get; }

    private GsisNumber(string value)
    {
        Value = value;
    }

    public static Result<GsisNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<GsisNumber>(EmployeeErrors.GsisNumberInvalidFormat);
        }

        var normalized = value.Trim();
        return Pattern().IsMatch(normalized)
            ? Result.Success(new GsisNumber(normalized))
            : Result.Failure<GsisNumber>(EmployeeErrors.GsisNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[0-9]{8,11}$")]
    private static partial Regex Pattern();
}
