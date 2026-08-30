using Hris.SharedKernel;

namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// A Philippine Social Security System number. See <see cref="TaxIdentificationNumber"/>'s
/// own remarks on why this validates digit count (10) only, not a specific
/// hyphen-grouping mask.
/// </summary>
public sealed class SssNumber : ValueObject
{
    public string Value { get; }

    private SssNumber(string value)
    {
        Value = value;
    }

    public static Result<SssNumber> Create(string? value)
    {
        var digits = PhilippineIdFormat.ExtractDigits(value);

        return digits is { Length: 10 }
            ? Result.Success(new SssNumber(digits))
            : Result.Failure<SssNumber>(ValidationErrors.SssNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
