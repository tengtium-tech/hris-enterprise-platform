using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// The number of approved occupants for a <see cref="Position"/>. Source:
/// docs/04-modules/position/domain/value-objects.md, Authorized Headcount
/// ("Non-negative validation. Business comparison. Capacity calculations."). Zero is
/// explicitly permitted (business-rules.md: "Authorized Headcount may be zero.").
/// </summary>
public sealed class AuthorizedHeadcount : ValueObject
{
    public int Value { get; }

    private AuthorizedHeadcount(int value)
    {
        Value = value;
    }

    public static Result<AuthorizedHeadcount> Create(int value)
    {
        return value < 0
            ? Result.Failure<AuthorizedHeadcount>(PositionErrors.AuthorizedHeadcountNegative)
            : Result.Success(new AuthorizedHeadcount(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
