using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Domain Events raised within the Leave module, per domain-events.md. Grouped in one
/// file per this module's own File Organization convention
/// (docs/08-devops/coding-standards.md); added to as each aggregate is built.
/// </summary>
public sealed record LeaveTypeDefined(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveTypeId LeaveTypeId,
    Guid TenantId,
    string Code,
    Guid ActorId) : IDomainEvent;

public sealed record LeaveTypeDeactivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveTypeId LeaveTypeId,
    Guid TenantId,
    Guid ActorId,
    string Reason) : IDomainEvent;

public sealed record LeavePolicyPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeavePolicyId LeavePolicyId,
    Guid TenantId,
    DateOnly EffectiveFrom,
    Guid ActorId) : IDomainEvent;

public sealed record LeavePolicyAssigned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeavePolicyId LeavePolicyId,
    Guid TenantId,
    PolicyAssignmentId PolicyAssignmentId,
    string ScopeTargetId,
    Guid ActorId) : IDomainEvent;

/// <summary>Raised on the version <see cref="LeavePolicy.Revise"/> is called against, naming the version that supersedes it.</summary>
public sealed record LeavePolicySuperseded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeavePolicyId LeavePolicyId,
    Guid TenantId,
    LeavePolicyId SupersededByLeavePolicyId,
    Guid ActorId) : IDomainEvent;
