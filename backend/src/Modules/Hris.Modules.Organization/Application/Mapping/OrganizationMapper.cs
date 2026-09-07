using Hris.Modules.Organization.Application.Dtos;
using Hris.Modules.Organization.Domain;

namespace Hris.Modules.Organization.Application.Mapping;

/// <summary>
/// Domain-to-DTO projection for this module's three Aggregate Roots, per
/// mapping.md's own convention: a plain static class, not a runtime reflection
/// mapper (AutoMapper and similar), the identical choice every other framework in
/// this codebase already makes.
/// </summary>
internal static class OrganizationMapper
{
    public static OrganizationDto ToDto(Domain.Organization organization) => new(
        organization.Id.Value,
        organization.TenantId,
        organization.Code.Value,
        organization.Name.Value,
        organization.LegalEntityId,
        organization.Description,
        organization.Status.ToString(),
        organization.CreatedAtUtc,
        organization.BusinessUnits.Select(ToDto).ToList(),
        organization.CostCenters.Select(ToDto).ToList());

    public static OrganizationSummaryDto ToSummaryDto(Domain.Organization organization) => new(
        organization.Id.Value, organization.Code.Value, organization.Name.Value, organization.Status.ToString());

    private static BusinessUnitDto ToDto(BusinessUnit businessUnit) => new(
        businessUnit.Id.Value,
        businessUnit.Name.Value,
        businessUnit.Status.ToString(),
        businessUnit.Divisions.Select(ToDto).ToList());

    private static DivisionDto ToDto(Division division) => new(
        division.Id.Value,
        division.Name.Value,
        division.Status.ToString(),
        division.Departments.Select(ToDto).ToList());

    private static DepartmentDto ToDto(Department department) => new(
        department.Id.Value,
        department.Name.Value,
        department.Code.Value,
        department.Status.ToString(),
        department.MergedIntoDepartmentId?.Value,
        department.SplitFromDepartmentId?.Value,
        department.Sections.Select(ToDto).ToList());

    private static SectionDto ToDto(Section section) => new(
        section.Id.Value, section.Name.Value, section.Status.ToString(), section.Teams.Select(ToDto).ToList());

    private static TeamDto ToDto(Team team) => new(team.Id.Value, team.Name.Value, team.Status.ToString());

    private static CostCenterDto ToDto(CostCenter costCenter) => new(
        costCenter.Id.Value, costCenter.Code.Value, costCenter.Description, costCenter.Status.ToString());

    public static WorkLocationDto ToDto(WorkLocation workLocation) => new(
        workLocation.Id.Value,
        workLocation.TenantId,
        workLocation.Code.Value,
        workLocation.Name,
        workLocation.OrganizationId,
        workLocation.LegalEntityId,
        ToDto(workLocation.Address),
        workLocation.TimeZone.Value,
        workLocation.Coordinates?.Latitude,
        workLocation.Coordinates?.Longitude,
        workLocation.Status.ToString(),
        workLocation.CreatedAtUtc);

    public static WorkLocationSummaryDto ToSummaryDto(WorkLocation workLocation) => new(
        workLocation.Id.Value, workLocation.Code.Value, workLocation.Name, workLocation.Status.ToString());

    private static AddressDto ToDto(Address address) => new(
        address.Line1, address.Line2, address.City, address.ProvinceOrState, address.PostalCode, address.Country);

    public static LegalEntityDto ToDto(LegalEntity legalEntity) => new(
        legalEntity.Id.Value,
        legalEntity.TenantId,
        legalEntity.Code,
        legalEntity.Name,
        legalEntity.RegisteredBusinessName,
        legalEntity.BusinessRegistrationNumber.Value,
        legalEntity.TaxIdentificationNumber?.Value,
        legalEntity.Country,
        legalEntity.Currency?.Value,
        legalEntity.RegisteredAddress is null ? null : ToDto(legalEntity.RegisteredAddress),
        legalEntity.Status.ToString(),
        legalEntity.CreatedAtUtc);

    public static LegalEntitySummaryDto ToSummaryDto(LegalEntity legalEntity) => new(
        legalEntity.Id.Value, legalEntity.Code, legalEntity.Name, legalEntity.Status.ToString());
}
