using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Every Domain Event this module's four Aggregate Roots raise. Source:
/// docs/04-modules/position/domain/domain-events.md, which names each of these by
/// section. <see cref="Guid"/> TenantId appears only on the *Created events, the
/// identical selective-inclusion precedent <c>OrganizationDomainEvents</c> already
/// establishes: a consumer reacting to a later lifecycle event on an already-known
/// record has no need to be told its tenant a second time.
/// </summary>
public sealed record PositionCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    PositionId PositionId,
    Guid TenantId,
    string PositionNumber,
    string PositionTitle,
    Guid OrganizationId) : IDomainEvent;

public sealed record PositionUpdated(Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId) : IDomainEvent;

public sealed record PositionActivated(Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId) : IDomainEvent;

public sealed record PositionDeactivated(Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId) : IDomainEvent;

public sealed record PositionArchived(Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId) : IDomainEvent;

public sealed record ReportingPositionAssigned(
    Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId, Guid ReportingPositionId) : IDomainEvent;

public sealed record ReportingPositionChanged(
    Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId, Guid ReportingPositionId) : IDomainEvent;

public sealed record ReportingPositionRemoved(
    Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId) : IDomainEvent;

public sealed record AuthorizedHeadcountChanged(
    Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId, int NewAuthorizedHeadcount) : IDomainEvent;

public sealed record PositionMarkedVacant(Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId) : IDomainEvent;

public sealed record PositionFilled(Guid EventId, DateTimeOffset OccurredOnUtc, PositionId PositionId) : IDomainEvent;

public sealed record JobFamilyCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobFamilyId JobFamilyId,
    Guid TenantId,
    string JobFamilyCode,
    string JobFamilyName) : IDomainEvent;

public sealed record JobFamilyUpdated(Guid EventId, DateTimeOffset OccurredOnUtc, JobFamilyId JobFamilyId) : IDomainEvent;

public sealed record JobFamilyActivated(Guid EventId, DateTimeOffset OccurredOnUtc, JobFamilyId JobFamilyId) : IDomainEvent;

public sealed record JobFamilyDeactivated(Guid EventId, DateTimeOffset OccurredOnUtc, JobFamilyId JobFamilyId) : IDomainEvent;

public sealed record JobFamilyArchived(Guid EventId, DateTimeOffset OccurredOnUtc, JobFamilyId JobFamilyId) : IDomainEvent;

public sealed record JobClassificationCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobClassificationId JobClassificationId,
    Guid TenantId,
    string JobClassificationCode,
    string JobClassificationName) : IDomainEvent;

public sealed record JobClassificationUpdated(
    Guid EventId, DateTimeOffset OccurredOnUtc, JobClassificationId JobClassificationId) : IDomainEvent;

public sealed record JobClassificationActivated(
    Guid EventId, DateTimeOffset OccurredOnUtc, JobClassificationId JobClassificationId) : IDomainEvent;

public sealed record JobClassificationDeactivated(
    Guid EventId, DateTimeOffset OccurredOnUtc, JobClassificationId JobClassificationId) : IDomainEvent;

public sealed record JobClassificationArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, JobClassificationId JobClassificationId) : IDomainEvent;

public sealed record JobGradeCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobGradeId JobGradeId,
    Guid TenantId,
    string JobGradeCode,
    string JobGradeName) : IDomainEvent;

public sealed record JobGradeUpdated(Guid EventId, DateTimeOffset OccurredOnUtc, JobGradeId JobGradeId) : IDomainEvent;

public sealed record JobGradeActivated(Guid EventId, DateTimeOffset OccurredOnUtc, JobGradeId JobGradeId) : IDomainEvent;

public sealed record JobGradeDeactivated(Guid EventId, DateTimeOffset OccurredOnUtc, JobGradeId JobGradeId) : IDomainEvent;

public sealed record JobGradeArchived(Guid EventId, DateTimeOffset OccurredOnUtc, JobGradeId JobGradeId) : IDomainEvent;
