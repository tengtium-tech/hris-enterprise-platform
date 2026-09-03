using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// A Number Series' own stable, globally unique identifier -- what a caller requesting
/// a number references, distinct from its <see cref="NumberPrefix"/> (the doc's own
/// examples use a readable key like "Employee Numbers" alongside a separate short
/// prefix like "EMP"). Validated for shape only (required, reasonable length) -- the
/// document gives no explicit key format to enforce, the identical reasoning
/// <c>ExtensionPointKey</c>'s own remarks state for itself.
/// </summary>
public sealed class SeriesKey : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private SeriesKey(string value)
    {
        Value = value;
    }

    public static Result<SeriesKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<SeriesKey>(NumberingErrors.SeriesKeyRequired);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > _maxLength)
        {
            return Result.Failure<SeriesKey>(NumberingErrors.SeriesKeyTooLong);
        }

        return Result.Success(new SeriesKey(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
