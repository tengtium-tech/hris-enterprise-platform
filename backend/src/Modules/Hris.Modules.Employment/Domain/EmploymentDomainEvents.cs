using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Every Domain Event this module's three Aggregate Roots raise. Source:
/// docs/04-modules/employment/domain/domain-events.md, which names each of these by
/// section. <see cref="Guid"/> TenantId appears only on the *Created events, the
/// identical selective-inclusion precedent <c>OrganizationDomainEvents</c> and
/// <c>PositionDomainEvents</c> already establish.
///
/// A handful of events have no name given anywhere in domain-events.md and are
/// inferred here to cover a transition this module's own aggregates.md and
/// employment-contracts.md both require: <see cref="EmploymentSeconded"/> (the
/// SecondEmploymentCommand's own result -- domain-events.md names ProbationStarted
/// through EmployeeRehired but never a secondment event), and the Contract Lifecycle's
/// own Draft-to-Approved, Approved-to-Effective, and Draft/Approved-to-Cancelled
/// transitions (<see cref="EmploymentContractApproved"/>,
/// <see cref="EmploymentContractEffective"/>, <see cref="EmploymentContractCancelled"/>,
/// <see cref="EmploymentContractSuperseded"/> -- domain-events.md's own Employment
/// Contract Events section names only Created, Renewed, Extended, Expired, and
/// Closed, omitting the four other stages ADR-0008's own Contract Lifecycle table
/// requires). Named following this document's own stated convention
/// (<c>&lt;Entity&gt;&lt;PastTenseVerb&gt;</c>).
/// </summary>

// Employment.
public sealed record EmploymentCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentId EmploymentId,
    Guid TenantId,
    Guid EmployeeId,
    string EmploymentNumber,
    string EmploymentType,
    string EmploymentCategory) : IDomainEvent;

public sealed record EmploymentActivated(Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId) : IDomainEvent;

public sealed record EmploymentTypeChanged(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId, string PreviousType, string NewType) : IDomainEvent;

public sealed record EmploymentCategoryChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentId EmploymentId,
    string PreviousCategory,
    string NewCategory) : IDomainEvent;

public sealed record EmploymentCompensationChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentId EmploymentId,
    Guid EmployeeId,
    CompensationRecordId? PreviousCompensationRecordId,
    CompensationRecordId NewCompensationRecordId,
    DateOnly EffectiveDate,
    CompensationChangeSource ChangeSource) : IDomainEvent;

public sealed record ProbationStarted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentId EmploymentId,
    ProbationRecordId ProbationRecordId,
    DateOnly StartDate,
    DateOnly ExpectedEvaluationDate) : IDomainEvent;

public sealed record ProbationExtended(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentId EmploymentId,
    ProbationRecordId ProbationRecordId,
    DateOnly NewExpectedEvaluationDate) : IDomainEvent;

public sealed record EmploymentConfirmed(Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId) : IDomainEvent;

public sealed record ProbationFailed(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId, ProbationRecordId ProbationRecordId) : IDomainEvent;

public sealed record EmploymentSuspended(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId, string Reason, DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmploymentReinstated(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId, DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmploymentSeconded(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId, DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmploymentSeparated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentId EmploymentId,
    SeparationType SeparationReason,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmployeeRehired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentId NewEmploymentId,
    Guid EmployeeId,
    Guid PriorEmploymentId,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record ConcurrentEmploymentCreated(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId, Guid EmployeeId, Guid PrimaryEmploymentId)
    : IDomainEvent;

public sealed record PrimaryEmploymentChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EmployeeId,
    EmploymentId NewPrimaryEmploymentId,
    Guid PreviousPrimaryEmploymentId) : IDomainEvent;

public sealed record ConcurrentEmploymentEnded(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentId EmploymentId, Guid EmployeeId) : IDomainEvent;

// EmploymentContract.
public sealed record EmploymentContractCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentContractId EmploymentContractId,
    Guid TenantId,
    Guid EmploymentId,
    DateOnly StartDate,
    DateOnly? EndDate) : IDomainEvent;

public sealed record EmploymentContractApproved(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentContractId EmploymentContractId) : IDomainEvent;

public sealed record EmploymentContractEffective(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentContractId EmploymentContractId) : IDomainEvent;

public sealed record EmploymentContractRenewed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentContractId EmploymentContractId,
    ContractRenewalId ContractRenewalId,
    DateOnly NewStartDate,
    DateOnly? NewEndDate) : IDomainEvent;

public sealed record EmploymentContractExtended(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentContractId EmploymentContractId,
    ContractExtensionId ContractExtensionId,
    DateOnly NewEndDate) : IDomainEvent;

public sealed record EmploymentContractExpired(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentContractId EmploymentContractId) : IDomainEvent;

public sealed record EmploymentContractSuperseded(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentContractId EmploymentContractId) : IDomainEvent;

public sealed record EmploymentContractClosed(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentContractId EmploymentContractId) : IDomainEvent;

public sealed record EmploymentContractCancelled(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmploymentContractId EmploymentContractId) : IDomainEvent;

// EmploymentAssignment.
public sealed record EmploymentAssignmentCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentAssignmentId EmploymentAssignmentId,
    Guid TenantId,
    Guid EmploymentId,
    Guid PositionId,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmploymentAssignmentChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentAssignmentId EmploymentAssignmentId,
    Guid EmploymentId,
    Guid PreviousPositionId,
    Guid NewPositionId,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmploymentPromoted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EmploymentId,
    Guid PreviousPositionId,
    Guid NewPositionId,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmploymentDemoted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EmploymentId,
    Guid PreviousPositionId,
    Guid NewPositionId,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record EmploymentTransferred(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EmploymentId,
    Guid? PreviousOrganizationalUnitId,
    Guid? NewOrganizationalUnitId,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record ReportingManagerChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentAssignmentId EmploymentAssignmentId,
    Guid EmploymentId,
    Guid? PreviousReportingManagerEmploymentId,
    Guid NewReportingManagerEmploymentId) : IDomainEvent;

public sealed record EmploymentAssignmentEnded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    EmploymentAssignmentId EmploymentAssignmentId,
    Guid EmploymentId,
    DateOnly EffectiveDate) : IDomainEvent;
