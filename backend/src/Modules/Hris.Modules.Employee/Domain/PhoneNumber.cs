using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Source: docs/04-modules/employee/domain/value-objects.md's PhoneNumber
/// ("Valid international format. Country-aware validation. Normalized storage").
/// This Sprint validates a general international shape (leading optional '+',
/// digits, spaces, and dashes) rather than genuine per-country numbering-plan
/// validation, which would require a Statutory Reference Data / libphonenumber-
/// equivalent dependency this Sprint does not add -- a documented gap, not a
/// silently-invented rule. Reused for mobile and telephone numbers alike, and for
/// <see cref="EmergencyContact"/>'s own phone field. Single-field, so mapped via
/// <c>HasConversion</c>.
/// </summary>
public sealed partial class PhoneNumber : ValueObject
{
    private const int _maxLength = 30;

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PhoneNumber>(EmployeeErrors.PhoneNumberInvalidFormat);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        if (normalized.Length > _maxLength)
        {
            return Result.Failure<PhoneNumber>(EmployeeErrors.PhoneNumberTooLong);
        }

        return PhonePattern().IsMatch(normalized)
            ? Result.Success(new PhoneNumber(normalized))
            : Result.Failure<PhoneNumber>(EmployeeErrors.PhoneNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"^\+?[0-9][0-9 \-]{5,}$")]
    private static partial Regex PhonePattern();
}
