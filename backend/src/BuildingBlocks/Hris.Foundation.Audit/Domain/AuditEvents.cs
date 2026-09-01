using Hris.SharedKernel;

namespace Hris.Foundation.Audit.Domain;

/// <summary>
/// One of audit-framework.md's own six listed Domain Events -- deliberately the only
/// one added alongside this framework's Application layer.
/// <c>AuditRecordIndexed</c>/<c>AuditSearchExecuted</c>/<c>AuditReportGenerated</c>/
/// <c>AuditArchived</c>/<c>AuditRetentionApplied</c> each describe a capability
/// (indexing infrastructure, reporting, archival, retention policy) this Sprint does
/// not build -- raising an event for an operation that does not exist would describe
/// a capability this framework does not have, the same reasoning
/// <c>RoleCreated</c>/<c>PolicyCreated</c> were left out of Authorization Framework's
/// own event set.
///
/// Not raised by <see cref="AuditRecord"/> itself via <c>AddDomainEvent</c> --
/// <see cref="AuditRecord"/> is a plain <see cref="Entity{TId}"/>, not an
/// <see cref="AggregateRoot{TId}"/>, by that type's own deliberate design (no
/// mutation, no lifecycle, nothing an aggregate's own invariant-guarding exists for).
/// <c>IAuditRecorder</c>, this framework's own Application-layer facade, constructs
/// this event directly after a successful <see cref="AuditRecord.Create"/>, the same
/// "a Domain Service may raise a Domain Event without owning an AggregateRoot's own
/// event collection" shape Authorization Framework's own <c>AuthorizationDecision</c>
/// already establishes.
/// </summary>
public sealed record AuditRecordCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    AuditRecordId AuditRecordId,
    AuditCategory Category,
    string BusinessEntity,
    string EntityIdentifier) : IDomainEvent;
