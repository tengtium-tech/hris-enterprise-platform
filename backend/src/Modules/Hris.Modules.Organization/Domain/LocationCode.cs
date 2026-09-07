using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The unique short code identifying a <see cref="WorkLocation"/> (examples: "HQ",
/// "BR001", "REMOTE"). Source:
/// docs/04-modules/organization/domain/value-objects.md, LocationCode. Normalized to
/// upper invariant. Uniqueness (LOC-001, tenant-wide) is checked by the Application
/// layer against the repository, not by this type.
/// </summary>
public sealed class LocationCode : ValueObject
{
    private const int _maxLength = 20;

    public string Value { get; }

    private LocationCode(string value)
    {
        Value = value;
    }

    public static Result<LocationCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<LocationCode>(OrganizationErrors.LocationCodeRequired);
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized.Length > _maxLength
            ? Result.Failure<LocationCode>(OrganizationErrors.LocationCodeRequired)
            : Result.Success(new LocationCode(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
