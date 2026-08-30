using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// The exact catalog from configuration-framework.md's own Domain Events section.
/// Each is raised only by <see cref="ConfigurationSetting"/> after the state change it
/// describes has already succeeded, never before
/// (docs/02-architecture/04-domain-driven-design/domain-events.md, "Event Timing").
///
/// <see cref="ConfigurationValidationFailed"/> is not an exception to that rule: the
/// operation it reports -- running validation rules against a draft -- completes
/// successfully as an operation, and "the draft is invalid" is the business fact the
/// event records, the same way a <c>LeaveRejected</c> event records a completed
/// rejection rather than a failed approval (domain-events.md's own catalog treats
/// rejection as a legitimate past-tense fact, not an operation failure).
/// </summary>
public sealed record ConfigurationCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConfigurationId ConfigurationId,
    ConfigurationKey Key,
    ConfigurationScope Scope) : IDomainEvent;

public sealed record ConfigurationUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConfigurationId ConfigurationId,
    ConfigurationVersionId VersionId,
    int VersionNumber) : IDomainEvent;

public sealed record ConfigurationPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConfigurationId ConfigurationId,
    ConfigurationVersionId VersionId,
    int VersionNumber,
    DateOnly EffectiveDate) : IDomainEvent;

public sealed record ConfigurationActivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConfigurationId ConfigurationId,
    ConfigurationVersionId VersionId,
    int VersionNumber) : IDomainEvent;

public sealed record ConfigurationDeprecated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConfigurationId ConfigurationId,
    ConfigurationVersionId VersionId,
    int VersionNumber) : IDomainEvent;

public sealed record ConfigurationArchived(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConfigurationId ConfigurationId,
    ConfigurationVersionId VersionId,
    int VersionNumber) : IDomainEvent;

public sealed record ConfigurationValidationFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ConfigurationId ConfigurationId,
    ConfigurationVersionId VersionId,
    string Reason) : IDomainEvent;
