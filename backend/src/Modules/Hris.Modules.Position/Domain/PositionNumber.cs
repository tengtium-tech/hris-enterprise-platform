using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The permanent business identifier of a <see cref="Position"/> (examples:
/// "POS-000001", "ENG-001245"). Source:
/// docs/04-modules/position/domain/position-numbering.md ("A Position Number is a
/// business identifier... It remains unchanged throughout the Position lifecycle").
/// Immutable once assigned -- <see cref="Position.Create"/> is the only place this
/// type is ever constructed for a given Position. Normalized to upper invariant, the
/// same convention <c>CostCenterCode</c> already establishes for a short business
/// code. Uniqueness (position-numbering.md: "Position Numbers must be unique within
/// a tenant") is checked by the Application layer against the repository, not by
/// this type or by <see cref="Position"/> itself.
/// </summary>
public sealed class PositionNumber : ValueObject
{
    private const int _maxLength = 50;

    public string Value { get; }

    private PositionNumber(string value)
    {
        Value = value;
    }

    public static Result<PositionNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PositionNumber>(PositionErrors.PositionNumberRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length > _maxLength
            ? Result.Failure<PositionNumber>(PositionErrors.PositionNumberTooLong)
            : Result.Success(new PositionNumber(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
