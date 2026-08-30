using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// The outcome of <see cref="AuthorizationEvaluator.EvaluateAsync"/>: authorization-framework.md's
/// own "Permission Evaluation" section states this must be one of exactly two
/// outcomes ("Default behavior should deny access when authorization cannot be
/// determined") -- there is no third, ambiguous state.
///
/// <see cref="AuthorizationEvaluator"/> is a stateless Domain Service, not an
/// <see cref="AggregateRoot{TId}"/>, so it has no <c>DomainEvents</c> collection of
/// its own to raise into (domain-events.md's own Event Ownership section reserves
/// that mechanism for Aggregates). domain-services.md's "A Domain Service may...
/// Raise Domain Events (when appropriate)" is honored here by having the decision
/// *carry* the corresponding <see cref="AuthorizationEvaluated"/>/<see cref="AuthorizationDenied"/>
/// event for its caller to publish through Event Framework's <c>IEventPublisher</c>,
/// rather than pretending this type owns an event collection it does not.
/// </summary>
public sealed class AuthorizationDecision
{
    public bool IsAllowed { get; }

    public string? DenialReason { get; }

    public IDomainEvent Event { get; }

    private AuthorizationDecision(bool isAllowed, string? denialReason, IDomainEvent domainEvent)
    {
        IsAllowed = isAllowed;
        DenialReason = denialReason;
        Event = domainEvent;
    }

    public static AuthorizationDecision Allow(UserAccountId principalId, PermissionKey requestedPermission, DateTimeOffset nowUtc) =>
        new(true, null, new AuthorizationEvaluated(Guid.NewGuid(), nowUtc, principalId, requestedPermission, IsAllowed: true));

    public static AuthorizationDecision Deny(UserAccountId principalId, PermissionKey requestedPermission, string reason, DateTimeOffset nowUtc) =>
        new(false, reason, new AuthorizationDenied(Guid.NewGuid(), nowUtc, principalId, requestedPermission, reason));
}
