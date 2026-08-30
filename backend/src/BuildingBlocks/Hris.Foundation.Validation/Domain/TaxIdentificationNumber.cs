using Hris.SharedKernel;

namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// A Philippine BIR Tax Identification Number, per validation-framework.md's
/// "Government Identifier Format" validation rule and
/// value-objects.md's own Government Identifiers section ("TIN... Each validates its
/// own format").
///
/// Validates digit count only (9, or 12 with a branch/RDO code) after stripping
/// hyphens and spaces -- <b>not</b> a specific hyphen-grouping mask. Neither this
/// document nor docs/03-foundation/statutory-reference-data.md (checked directly;
/// it specifies contribution *schedules*, not identifier *formats*) states the BIR's
/// exact canonical grouping, and asserting one from general knowledge alone risks
/// rejecting valid numbers or accepting invalid ones -- worse than the narrower
/// check implemented here. Tighten this once an authoritative BIR format reference
/// is available.
/// </summary>
public sealed class TaxIdentificationNumber : ValueObject
{
    public string Value { get; }

    private TaxIdentificationNumber(string value)
    {
        Value = value;
    }

    public static Result<TaxIdentificationNumber> Create(string? value)
    {
        var digits = PhilippineIdFormat.ExtractDigits(value);

        return digits is { Length: 9 or 12 }
            ? Result.Success(new TaxIdentificationNumber(digits))
            : Result.Failure<TaxIdentificationNumber>(ValidationErrors.TaxIdentificationNumberInvalidFormat);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
