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
