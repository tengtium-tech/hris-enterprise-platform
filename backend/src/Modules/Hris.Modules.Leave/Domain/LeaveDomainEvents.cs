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

/// <summary>Raised whenever any <see cref="LeaveLedgerEntry"/> is written, of any <see cref="LeaveLedgerEntryType"/>.</summary>
public sealed record LeaveLedgerEntryAppended(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    LeaveLedgerEntryId LeaveLedgerEntryId,
    LeaveLedgerEntryType EntryType,
    decimal Amount) : IDomainEvent;

public sealed record LeaveBalanceAccrued(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    decimal Amount,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record LeaveCarriedOver(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    decimal Amount,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record LeaveForfeited(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    decimal Amount,
    DateOnly EffectiveDate) : IDomainEvent;

/// <summary>A verification/repair resummation completed (LV-025) — raised whether or not the total actually changed.</summary>
public sealed record LeaveBalanceRecalculated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LeaveBalanceId LeaveBalanceId,
    Guid TenantId,
    Guid ActorId,
    bool WasCorrected) : IDomainEvent;
