using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Social Security System number (private-sector employees). Source:
/// docs/04-modules/employee/domain/employee-identification.md's Identifier
/// Catalogue. See <see cref="Tin"/>'s own remarks on the format-validation
/// boundary this Sprint draws. Mutually exclusive with <see cref="GsisNumber"/> by
/// employing legal entity sector -- a fact this module does not enforce, since
/// Employee does not reference a legal entity (employee-identification.md:
/// "determined by the employing legal entity's sector classification, not by
/// employee choice", owned by the Employment module's own sector data).
/// </summary>
public sealed partial class SssNumber : ValueObject
{
    public string Value { get; }

    private SssNumber(string value)
    {
        Value = value;
    }

    public static Result<SssNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<SssNumber>(EmployeeErrors.SssNumberInvalidFormat);
        }

        var normalized = value.Trim();
        return Pattern().IsMatch(normalized)
            ? Result.Success(new SssNumber(normalized))
            : Result.Failure<SssNumber>(EmployeeErrors.SssNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[0-9]{2}-?[0-9]{7}-?[0-9]{1}$")]
    private static partial Regex Pattern();
}
