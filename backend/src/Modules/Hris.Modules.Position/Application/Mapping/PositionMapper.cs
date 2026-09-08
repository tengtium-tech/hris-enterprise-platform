using Hris.Modules.Position.Application.Dtos;
using Hris.Modules.Position.Domain;

namespace Hris.Modules.Position.Application.Mapping;

/// <summary>
/// Domain-to-DTO projection for this module's four Aggregate Roots, per
/// mapping.md's own convention: a plain static class, not a runtime reflection
/// mapper, the identical choice <c>OrganizationMapper</c> already makes.
/// </summary>
internal static class PositionMapper
{
    public static PositionDto ToDto(Domain.Position position) => new(
        position.Id.Value,
        position.TenantId,
        position.Number.Value,
        position.Title.Value,
        position.PositionType.Value,
        position.OrganizationId,
        position.LegalEntityId,
        position.BusinessUnitId,
        position.DivisionId,
        position.DepartmentId,
        position.SectionId,
        position.TeamId,
        position.WorkLocationId,
        position.CostCenterId,
        position.JobFamilyId,
        position.JobClassificationId,
        position.JobGradeId,
        position.ReportingPositionId,
        position.AuthorizedHeadcount.Value,
        position.Status.ToString(),
        position.VacancyStatus.ToString(),
        position.CreatedAtUtc);

    public static PositionSummaryDto ToSummaryDto(Domain.Position position) => new(
        position.Id.Value,
        position.Number.Value,
        position.Title.Value,
        position.OrganizationId,
        position.ReportingPositionId,
        position.Status.ToString(),
        position.VacancyStatus.ToString());

    public static JobFamilyDto ToDto(JobFamily jobFamily) => new(
        jobFamily.Id.Value,
        jobFamily.TenantId,
        jobFamily.Code.Value,
        jobFamily.Name.Value,
        jobFamily.Description,
        jobFamily.Status.ToString(),
        jobFamily.CreatedAtUtc);

    public static JobFamilySummaryDto ToSummaryDto(JobFamily jobFamily) => new(
        jobFamily.Id.Value, jobFamily.Code.Value, jobFamily.Name.Value, jobFamily.Status.ToString());

    public static JobClassificationDto ToDto(JobClassification jobClassification) => new(
        jobClassification.Id.Value,
        jobClassification.TenantId,
        jobClassification.Code.Value,
        jobClassification.Name.Value,
        jobClassification.Description,
        jobClassification.Status.ToString(),
        jobClassification.CreatedAtUtc);

    public static JobClassificationSummaryDto ToSummaryDto(JobClassification jobClassification) => new(
        jobClassification.Id.Value, jobClassification.Code.Value, jobClassification.Name.Value, jobClassification.Status.ToString());

    public static JobGradeDto ToDto(JobGrade jobGrade) => new(
        jobGrade.Id.Value,
        jobGrade.TenantId,
        jobGrade.Code.Value,
        jobGrade.Name.Value,
        jobGrade.Description,
        jobGrade.OrganizationalLevel,
        jobGrade.Status.ToString(),
        jobGrade.CreatedAtUtc);

    public static JobGradeSummaryDto ToSummaryDto(JobGrade jobGrade) => new(
        jobGrade.Id.Value, jobGrade.Code.Value, jobGrade.Name.Value, jobGrade.Status.ToString());
}
