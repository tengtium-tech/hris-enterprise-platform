using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// The final, human-facing identifier string an <see cref="IssuedNumber"/> carries once
/// <see cref="IssuedNumber.Reserve"/> assembles it via <see cref="NumberFormat.Format"/>
/// -- e.g. "EMP-2026-000123". A thin validated wrapper, not a parser: this type does
/// not decompose a formatted number back into its own prefix/year/sequence components,
/// since nothing in this framework's own build needs that reverse operation.
/// </summary>
public sealed class FormattedNumber : ValueObject
{
    private const int _maxLength = 100;

    public string Value { get; }

    private FormattedNumber(string value)
    {
        Value = value;
    }

    public static Result<FormattedNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<FormattedNumber>(NumberingErrors.FormattedNumberRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength)
        {
            return Result.Failure<FormattedNumber>(NumberingErrors.FormattedNumberTooLong);
        }

        return Result.Success(new FormattedNumber(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
