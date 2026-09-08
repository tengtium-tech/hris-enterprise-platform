namespace Hris.Modules.Employment.Application.Dtos;

public sealed record EmploymentDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    string Number,
    string EmploymentType,
    string Category,
    string LifecycleStage,
    string OperationalStatus,
    bool IsPrimary,
    Guid? PriorEmploymentId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<EmploymentStatusChangeDto> StatusChanges,
    IReadOnlyList<ProbationRecordDto> ProbationRecords,
    IReadOnlyList<CompensationRecordDto> CompensationRecords,
    SeparationRecordDto? SeparationRecord);

public sealed record EmploymentSummaryDto(
    Guid Id,
    Guid EmployeeId,
    string Number,
    string EmploymentType,
    string Category,
    string LifecycleStage,
    string OperationalStatus,
    bool IsPrimary);

public sealed record EmploymentStatusChangeDto(
    Guid Id, string PreviousStatus, string NewStatus, DateOnly EffectiveDate, string Reason);

public sealed record ProbationRecordDto(
    Guid Id, DateOnly StartDate, int DurationDays, DateOnly ExpectedEvaluationDate, string Outcome, int ExtensionCount);

public sealed record CompensationRecordDto(
    Guid Id, decimal Amount, string CurrencyCode, string Basis, DateOnly EffectiveStartDate, DateOnly? EffectiveEndDate,
    string ChangeSource, string? ApprovalReference);

public sealed record SeparationRecordDto(
    Guid Id, string SeparationType, string? TerminationReason, DateOnly LastWorkingDate, DateOnly EffectiveSeparationDate);
