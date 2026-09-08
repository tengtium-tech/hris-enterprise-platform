namespace Hris.Modules.Position.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetPositionQuery</c> returns.
/// </summary>
public sealed record PositionDto(
    Guid PositionId,
    Guid TenantId,
    string Number,
    string Title,
    string PositionType,
    Guid OrganizationId,
    Guid? LegalEntityId,
    Guid? BusinessUnitId,
    Guid? DivisionId,
    Guid? DepartmentId,
    Guid? SectionId,
    Guid? TeamId,
    Guid? WorkLocationId,
    Guid? CostCenterId,
    Guid JobFamilyId,
    Guid JobClassificationId,
    Guid JobGradeId,
    Guid? ReportingPositionId,
    int AuthorizedHeadcount,
    string Status,
    string VacancyStatus,
    DateTimeOffset CreatedAtUtc);

public sealed record PositionSummaryDto(
    Guid PositionId,
    string Number,
    string Title,
    Guid OrganizationId,
    Guid? ReportingPositionId,
    string Status,
    string VacancyStatus);
