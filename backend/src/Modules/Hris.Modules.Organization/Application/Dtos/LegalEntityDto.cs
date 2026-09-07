namespace Hris.Modules.Organization.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetLegalEntityQuery</c> returns.
/// </summary>
public sealed record LegalEntityDto(
    Guid LegalEntityId,
    Guid TenantId,
    string Code,
    string Name,
    string? RegisteredBusinessName,
    string BusinessRegistrationNumber,
    string? TaxIdentificationNumber,
    string Country,
    string? Currency,
    AddressDto? RegisteredAddress,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record LegalEntitySummaryDto(Guid LegalEntityId, string Code, string Name, string Status);
