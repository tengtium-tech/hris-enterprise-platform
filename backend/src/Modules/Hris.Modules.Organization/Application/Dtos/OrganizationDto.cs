namespace Hris.Modules.Organization.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetOrganizationQuery</c> returns, per dto-design.md's own
/// convention -- the full nested hierarchy
/// (BusinessUnit -&gt; Division -&gt; Department -&gt; Section -&gt; Team, plus Cost
/// Centers) in one payload, since a client rendering an organization chart needs
/// the whole tree in one round trip rather than five separate list calls.
/// </summary>
public sealed record OrganizationDto(
    Guid OrganizationId,
    Guid TenantId,
    string Code,
    string Name,
    Guid? LegalEntityId,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<BusinessUnitDto> BusinessUnits,
    IReadOnlyList<CostCenterDto> CostCenters);

public sealed record BusinessUnitDto(
    Guid BusinessUnitId, string Name, string Status, IReadOnlyList<DivisionDto> Divisions);

public sealed record DivisionDto(
    Guid DivisionId, string Name, string Status, IReadOnlyList<DepartmentDto> Departments);

public sealed record DepartmentDto(
    Guid DepartmentId,
    string Name,
    string Code,
    string Status,
    Guid? MergedIntoDepartmentId,
    Guid? SplitFromDepartmentId,
    IReadOnlyList<SectionDto> Sections);

public sealed record SectionDto(Guid SectionId, string Name, string Status, IReadOnlyList<TeamDto> Teams);

public sealed record TeamDto(Guid TeamId, string Name, string Status);

public sealed record CostCenterDto(Guid CostCenterId, string Code, string? Description, string Status);

/// <summary>
/// The lighter-weight shape <c>ListOrganizationsQuery</c> returns per matching row,
/// per api-standards.md's own Response Shapes guidance that a list endpoint returns
/// a summary projection, not the full nested tree the single-resource endpoint does.
/// </summary>
public sealed record OrganizationSummaryDto(Guid OrganizationId, string Code, string Name, string Status);
