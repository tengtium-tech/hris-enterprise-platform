using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Home Development Mutual Fund (Pag-IBIG) number. Source:
/// docs/04-modules/employee/domain/employee-identification.md's Identifier
/// Catalogue. See <see cref="Tin"/>'s own remarks on the format-validation
/// boundary this Sprint draws.
/// </summary>
public sealed partial class PagIbigNumber : ValueObject
{
    public string Value { get; }

    private PagIbigNumber(string value)
    {
        Value = value;
    }

    public static Result<PagIbigNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PagIbigNumber>(EmployeeErrors.PagIbigNumberInvalidFormat);
        }

        var normalized = value.Trim();
        return Pattern().IsMatch(normalized)
            ? Result.Success(new PagIbigNumber(normalized))
            : Result.Failure<PagIbigNumber>(EmployeeErrors.PagIbigNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[0-9]{4}-?[0-9]{4}-?[0-9]{4}$")]
    private static partial Regex Pattern();
}
