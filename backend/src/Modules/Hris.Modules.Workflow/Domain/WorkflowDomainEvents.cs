using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Every Domain Event this module's three Aggregate Roots raise. Source:
/// docs/04-modules/workflow/domain/domain-events.md.
///
/// That document describes two families. The definition-lifecycle and delegation
/// events below are raised by aggregates this module owns, and are implemented
/// here. The approval-outcome family it also lists (ApprovalRequested,
/// ApprovalGranted, ApprovalRejected, ApprovalExempted, ApprovalEscalated, and
/// ApprovalReassignedFromDelegation) is deliberately absent: every one of those is
/// raised "as an instance progresses" and carries a WorkflowInstanceId, and
/// aggregates.md is explicit that workflow instances and approval tasks are not
/// aggregates in this module because they change on the engine's schedule rather
/// than the tenant's. None of this module's three roots holds instance state, so
/// none of them is able to raise those events; defining record types nothing can
/// raise would be a shape with no owner rather than an implementation. They belong
/// to the Workflow Engine integration this Sprint does not wire, tracked in
/// STATUS.md as a documented gap in the same way Employee's own deferred
/// auto-separation trigger is.
///
/// Every event carries tenant context (CTR-ISO-004) and, where an action was taken
/// by one, the acting user. Expiry carries no actor, because none acted (WR-043).
/// </summary>
public sealed record WorkflowDefinitionCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowDefinitionId WorkflowDefinitionId,
    Guid TenantId,
    Guid BusinessProcessId,
    string Name,
    Guid CreatedBy) : IDomainEvent;

public sealed record WorkflowDefinitionPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowDefinitionId WorkflowDefinitionId,
    Guid TenantId,
    int Version,
    Guid PublishedBy,
    int StepCount) : IDomainEvent;

/// <summary>
/// Raised when a change to a published definition creates a new version rather than
/// modifying it in place (CTR-WFL-005). Carried on the new version, whose
/// <see cref="NewVersion"/> is the one new instances will start against.
/// </summary>
public sealed record WorkflowDefinitionVersioned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowDefinitionId WorkflowDefinitionId,
    Guid TenantId,
    Guid LineageId,
    int PreviousVersion,
    int NewVersion,
    Guid ChangedBy) : IDomainEvent;

public sealed record WorkflowDefinitionDeprecated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowDefinitionId WorkflowDefinitionId,
    Guid TenantId,
    Guid DeprecatedBy,
    string Reason) : IDomainEvent;

/// <summary>
/// Raised when a tenant's approval governance changes. Carries a reason because a
/// policy change that stops requiring approval for a business process is a control
/// removal, and a control removal without a stated reason is the change most worth
/// having one for (commands.md).
/// </summary>
public sealed record ApprovalPolicyConfigured(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ApprovalPolicyId ApprovalPolicyId,
    Guid TenantId,
    Guid ConfiguredBy,
    string Reason) : IDomainEvent;

public sealed record ApprovalDelegationCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ApprovalDelegationId ApprovalDelegationId,
    Guid TenantId,
    Guid DelegatorUserAccountId,
    Guid DelegateUserAccountId,
    IReadOnlyList<Guid> Scope,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Reason) : IDomainEvent;

/// <summary>
/// The event approval routing acts on, never <see cref="ApprovalDelegationCreated"/>.
/// A delegation created two weeks ahead that took effect on creation would be a
/// two-week window where the delegate could act on the delegator's approvals before
/// the absence it was arranged for even began.
/// </summary>
public sealed record ApprovalDelegationActivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ApprovalDelegationId ApprovalDelegationId,
    Guid TenantId) : IDomainEvent;

/// <summary>Automatic at period end, with no actor recorded (WR-043).</summary>
public sealed record ApprovalDelegationExpired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ApprovalDelegationId ApprovalDelegationId,
    Guid TenantId) : IDomainEvent;

public sealed record ApprovalDelegationRevoked(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ApprovalDelegationId ApprovalDelegationId,
    Guid TenantId,
    Guid RevokedBy,
    string Reason) : IDomainEvent;
