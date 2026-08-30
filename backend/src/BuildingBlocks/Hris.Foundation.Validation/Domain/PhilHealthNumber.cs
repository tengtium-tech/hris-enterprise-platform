using Hris.SharedKernel;

namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// A Philippine PhilHealth Identification Number. See <see cref="TaxIdentificationNumber"/>'s
/// own remarks on why this validates digit count (12) only, not a specific
/// hyphen-grouping mask.
/// </summary>
public sealed class PhilHealthNumber : ValueObject
{
    public string Value { get; }

    private PhilHealthNumber(string value)
    {
        Value = value;
    }

    public static Result<PhilHealthNumber> Create(string? value)
    {
        var digits = PhilippineIdFormat.ExtractDigits(value);

        return digits is { Length: 12 }
            ? Result.Success(new PhilHealthNumber(digits))
            : Result.Failure<PhilHealthNumber>(ValidationErrors.PhilHealthNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
