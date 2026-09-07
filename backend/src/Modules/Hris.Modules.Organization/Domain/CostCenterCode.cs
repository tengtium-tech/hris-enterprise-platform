using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The unique short code identifying a <see cref="CostCenter"/> (examples: "CC100",
/// "CC200", "IT001"). Source:
/// docs/04-modules/organization/domain/value-objects.md, CostCenterCode. Normalized
/// to upper invariant. Uniqueness (CC-001, tenant-wide) is checked by the
/// Application layer against the repository, not by this type or by
/// <see cref="Organization"/> itself.
/// </summary>
public sealed class CostCenterCode : ValueObject
{
    private const int _maxLength = 20;

    public string Value { get; }

    private CostCenterCode(string value)
    {
        Value = value;
    }

    public static Result<CostCenterCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CostCenterCode>(OrganizationErrors.CostCenterCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length > _maxLength
            ? Result.Failure<CostCenterCode>(OrganizationErrors.CostCenterCodeRequired)
            : Result.Success(new CostCenterCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
