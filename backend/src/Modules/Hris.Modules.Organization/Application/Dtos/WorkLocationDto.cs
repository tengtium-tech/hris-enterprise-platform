namespace Hris.Modules.Organization.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetWorkLocationQuery</c> returns.
/// </summary>
public sealed record WorkLocationDto(
    Guid WorkLocationId,
    Guid TenantId,
    string Code,
    string Name,
    Guid OrganizationId,
    Guid? LegalEntityId,
    AddressDto Address,
    string TimeZone,
    double? Latitude,
    double? Longitude,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record AddressDto(
    string Line1, string? Line2, string City, string? ProvinceOrState, string? PostalCode, string Country);

public sealed record WorkLocationSummaryDto(Guid WorkLocationId, string Code, string Name, string Status);
