namespace Hris.Modules.Employment.Application.Dtos;

public sealed record EmploymentAssignmentDto(
    Guid Id,
    Guid TenantId,
    Guid EmploymentId,
    Guid PositionId,
    Guid? DepartmentId,
    Guid? BusinessUnitId,
    Guid? CostCenterId,
    Guid? WorkLocationId,
    Guid? LegalEntityId,
    string WorkArrangement,
    Guid? ReportingManagerEmploymentId,
    DateOnly EffectiveStartDate,
    bool IsEnded,
    DateOnly? EndedDate,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<AssignmentHistoryRecordDto> History,
    IReadOnlyList<ReportingAssignmentDto> ReportingHistory);

public sealed record AssignmentHistoryRecordDto(
    Guid Id,
    Guid PositionId,
    Guid? DepartmentId,
    Guid? BusinessUnitId,
    Guid? CostCenterId,
    Guid? WorkLocationId,
    Guid? LegalEntityId,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate);

public sealed record ReportingAssignmentDto(
    Guid Id, Guid ReportingManagerEmploymentId, DateOnly EffectiveStartDate, DateOnly? EffectiveEndDate);
