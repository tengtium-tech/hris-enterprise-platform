using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A geographic position, owned by <see cref="WorkLocation"/>. Source:
/// docs/04-modules/organization/domain/value-objects.md, GeographicLocation
/// ("Typical properties: Latitude, Longitude. Used when mapping work locations.").
/// Optional on <see cref="WorkLocation"/> since that Aggregate's own Responsibilities
/// list ("Maintain location information... address... time zone... operational
/// status") never names mapping coordinates as mandatory.
/// </summary>
public sealed class GeographicLocation : ValueObject
{
    public double Latitude { get; }

    public double Longitude { get; }

    private GeographicLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Result<GeographicLocation> Create(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return Result.Failure<GeographicLocation>(OrganizationErrors.GeographicCoordinateOutOfRange);
        }

        return Result.Success(new GeographicLocation(latitude, longitude));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
