using Hris.SharedKernel;

namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// A Philippine Pag-IBIG (HDMF) Membership Identification Number. See
/// <see cref="TaxIdentificationNumber"/>'s own remarks on why this validates digit
/// count (12) only, not a specific hyphen-grouping mask.
/// </summary>
public sealed class PagIbigNumber : ValueObject
{
    public string Value { get; }

    private PagIbigNumber(string value)
    {
        Value = value;
    }

    public static Result<PagIbigNumber> Create(string? value)
    {
        var digits = PhilippineIdFormat.ExtractDigits(value);

        return digits is { Length: 12 }
            ? Result.Success(new PagIbigNumber(digits))
            : Result.Failure<PagIbigNumber>(ValidationErrors.PagIbigNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
