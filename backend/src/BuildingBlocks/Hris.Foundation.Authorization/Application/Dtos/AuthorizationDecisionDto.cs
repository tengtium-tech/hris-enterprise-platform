namespace Hris.Foundation.Authorization.Application.Dtos;

/// <summary>
/// The read-side shape of a <see cref="Domain.AuthorizationDecision"/>, returned by
/// <c>CheckAuthorizationQuery</c>. Never carries the Domain-layer
/// <see cref="Hris.SharedKernel.IDomainEvent"/> the decision also carries -- that
/// event exists for a future auditing caller to publish, per
/// <see cref="Domain.AuthorizationDecision"/>'s own remarks, not for a query result a
/// UI or another module consumes.
/// </summary>
public sealed record AuthorizationDecisionDto(bool IsAllowed, string? DenialReason);
