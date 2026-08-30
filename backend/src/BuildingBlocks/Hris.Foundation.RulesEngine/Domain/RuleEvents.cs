using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// rules-engine.md's own seven listed Domain Events. No <c>RuleValidated</c> or
/// <c>RuleActivated</c> exists to pair with those two lifecycle transitions -- the
/// document's own catalog does not list either, unlike <c>ConfigurationActivated</c>
/// in Configuration Framework's equivalent catalog, so none is invented here; see
/// <see cref="RuleVersion.MarkValidated"/> and <see cref="RuleVersion.Activate"/>,
/// which still perform the transition without raising an event, matching the source
/// document exactly rather than a sibling framework's own shape.
/// </summary>
public sealed record RuleCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RuleDefinitionId RuleDefinitionId,
    RuleKey Key,
    string Category) : IDomainEvent;

public sealed record RuleUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RuleDefinitionId RuleDefinitionId,
    RuleVersionId VersionId,
    int VersionNumber) : IDomainEvent;

public sealed record RulePublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RuleDefinitionId RuleDefinitionId,
    RuleVersionId VersionId,
    int VersionNumber) : IDomainEvent;

public sealed record RuleDeprecated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RuleDefinitionId RuleDefinitionId,
    RuleVersionId VersionId) : IDomainEvent;

public sealed record RuleArchived(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RuleDefinitionId RuleDefinitionId,
    RuleVersionId VersionId) : IDomainEvent;

/// <summary>
/// Carried by a matched <see cref="RuleEvaluationResult"/> for its caller to publish,
/// the same "Domain Service may raise events, but has no event collection of its own
/// to add them to" pattern <c>AuthorizationEvaluated</c> established -- see that
/// type's own remarks.
/// </summary>
public sealed record RuleExecuted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RuleDefinitionId RuleDefinitionId,
    RuleVersionId VersionId,
    UserAccountId? InitiatedBy) : IDomainEvent;

public sealed record RuleEvaluationFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    RuleDefinitionId RuleDefinitionId,
    RuleVersionId VersionId,
    string Reason) : IDomainEvent;
