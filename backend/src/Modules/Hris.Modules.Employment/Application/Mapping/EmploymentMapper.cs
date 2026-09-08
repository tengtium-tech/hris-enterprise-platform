using Hris.Modules.Employment.Application.Dtos;
using Hris.Modules.Employment.Domain;

namespace Hris.Modules.Employment.Application.Mapping;

internal static class EmploymentMapper
{
    public static EmploymentDto ToDto(Domain.Employment employment)
    {
        return new EmploymentDto(
            employment.Id.Value,
            employment.TenantId,
            employment.EmployeeId,
            employment.Number.Value,
            employment.EmploymentType.Value,
            employment.Category.Value,
            employment.LifecycleStage.ToString(),
            employment.OperationalStatus.ToString(),
            employment.IsPrimary,
            employment.PriorEmploymentId,
            employment.CreatedAtUtc,
            employment.StatusChanges.Select(ToDto).ToList(),
            employment.ProbationRecords.Select(ToDto).ToList(),
            employment.CompensationRecords.Select(ToDto).ToList(),
            employment.SeparationRecord is null ? null : ToDto(employment.SeparationRecord));
    }

    public static EmploymentSummaryDto ToSummaryDto(Domain.Employment employment)
    {
        return new EmploymentSummaryDto(
            employment.Id.Value,
            employment.EmployeeId,
            employment.Number.Value,
            employment.EmploymentType.Value,
            employment.Category.Value,
            employment.LifecycleStage.ToString(),
            employment.OperationalStatus.ToString(),
            employment.IsPrimary);
    }

    private static EmploymentStatusChangeDto ToDto(EmploymentStatusChange statusChange)
    {
        return new EmploymentStatusChangeDto(
            statusChange.Id.Value, statusChange.PreviousStatus.ToString(), statusChange.NewStatus.ToString(),
            statusChange.EffectiveDate, statusChange.Reason);
    }

    private static ProbationRecordDto ToDto(ProbationRecord probation)
    {
        return new ProbationRecordDto(
            probation.Id.Value, probation.StartDate, probation.Duration.Days, probation.ExpectedEvaluationDate,
            probation.Outcome.ToString(), probation.ExtensionCount);
    }

    private static CompensationRecordDto ToDto(CompensationRecord compensation)
    {
        return new CompensationRecordDto(
            compensation.Id.Value, compensation.Amount.Amount, compensation.Amount.CurrencyCode,
            compensation.Amount.Basis.ToString(), compensation.EffectiveStartDate, compensation.EffectiveEndDate,
            compensation.ChangeSource.ToString(), compensation.ApprovalReference);
    }

    private static SeparationRecordDto ToDto(SeparationRecord separation)
    {
        return new SeparationRecordDto(
            separation.Id.Value, separation.SeparationType.ToString(), separation.TerminationReason?.Value,
            separation.LastWorkingDate, separation.EffectiveSeparationDate);
    }

    public static EmploymentContractDto ToDto(EmploymentContract contract)
    {
        return new EmploymentContractDto(
            contract.Id.Value,
            contract.TenantId,
            contract.EmploymentId,
            contract.ContractType.Value,
            contract.Period.StartDate,
            contract.Period.EndDate,
            contract.LifecycleStage.ToString(),
            contract.SupersedesContractId,
            contract.CreatedAtUtc,
            contract.Renewals.Select(ToDto).ToList(),
            contract.Extensions.Select(ToDto).ToList(),
            contract.Documents.Select(ToDto).ToList());
    }

    private static ContractRenewalDto ToDto(ContractRenewal renewal)
    {
        return new ContractRenewalDto(
            renewal.Id.Value, renewal.PreviousStartDate, renewal.PreviousEndDate, renewal.NewStartDate,
            renewal.NewEndDate, renewal.ApprovalReference);
    }

    private static ContractExtensionDto ToDto(ContractExtension extension)
    {
        return new ContractExtensionDto(extension.Id.Value, extension.PreviousEndDate, extension.NewEndDate, extension.Reason);
    }

    private static ContractDocumentDto ToDto(ContractDocument document)
    {
        return new ContractDocumentDto(document.Id.Value, document.DocumentType, document.StorageReference, document.Version);
    }

    public static EmploymentAssignmentDto ToDto(EmploymentAssignment assignment)
    {
        return new EmploymentAssignmentDto(
            assignment.Id.Value,
            assignment.TenantId,
            assignment.EmploymentId,
            assignment.PositionId,
            assignment.DepartmentId,
            assignment.BusinessUnitId,
            assignment.CostCenterId,
            assignment.WorkLocationId,
            assignment.LegalEntityId,
            assignment.WorkArrangement.ToString(),
            assignment.ReportingManagerEmploymentId,
            assignment.EffectiveStartDate,
            assignment.IsEnded,
            assignment.EndedDate,
            assignment.CreatedAtUtc,
            assignment.History.Select(ToDto).ToList(),
            assignment.ReportingHistory.Select(ToDto).ToList());
    }

    private static AssignmentHistoryRecordDto ToDto(AssignmentHistoryRecord history)
    {
        return new AssignmentHistoryRecordDto(
            history.Id.Value, history.PositionId, history.DepartmentId, history.BusinessUnitId, history.CostCenterId,
            history.WorkLocationId, history.LegalEntityId, history.EffectiveStartDate, history.EffectiveEndDate);
    }

    private static ReportingAssignmentDto ToDto(ReportingAssignment reporting)
    {
        return new ReportingAssignmentDto(
            reporting.Id.Value, reporting.ReportingManagerEmploymentId, reporting.EffectiveStartDate,
            reporting.EffectiveEndDate);
    }
}
