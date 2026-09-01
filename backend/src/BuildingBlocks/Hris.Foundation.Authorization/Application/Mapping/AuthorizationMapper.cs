using Hris.Foundation.Authorization.Application.Dtos;
using Hris.Foundation.Authorization.Domain;

namespace Hris.Foundation.Authorization.Application.Mapping;

/// <summary>
/// Maps <see cref="RoleAssignment"/>/<see cref="RolePermissionGrant"/>/<see cref="AuthorizationDecision"/>
/// to their query-side DTOs, by hand rather than through a registered Mapster profile
/// -- the identical deviation <c>ConfigurationMapper</c>, <c>IdentityMapper</c>, and
/// <c>EventMapper</c> state and justify for the same reason: every field here either
/// unwraps a Value Object or a strongly typed id, or converts an enum to its DTO-side
/// string.
/// </summary>
internal static class AuthorizationMapper
{
    public static AuthorizationDecisionDto ToDto(this AuthorizationDecision decision) =>
        new(decision.IsAllowed, decision.DenialReason);

    public static RoleAssignmentDto ToDto(this RoleAssignment assignment) => new(
        assignment.Id.Value,
        assignment.PrincipalId.Value,
        assignment.Role.ToString(),
        assignment.Scope.Level.ToString(),
        assignment.Scope.ScopeId,
        assignment.AssignmentType.ToString(),
        assignment.EffectiveDate,
        assignment.ExpirationDate,
        assignment.RevokedAtUtc is not null);

    public static PermissionGrantDto ToDto(this RolePermissionGrant grant) => new(
        grant.Id.Value,
        grant.Role.ToString(),
        grant.Permission.ResourceType,
        grant.Permission.Action.ToString(),
        grant.IsActive);
}
