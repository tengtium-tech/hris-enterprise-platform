using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// Implements authorization-framework.md's Permission Evaluation section and its own
/// Centralized Evaluation requirement: "All authorization is evaluated by this
/// framework... Application code must never make authorization decisions by
/// comparing role names" (`CTR-AUT-001`). A Domain Service per domain-services.md's
/// Decision Guide -- this spans every <see cref="RoleAssignment"/> and
/// <see cref="RolePermissionGrant"/> a principal's decision depends on, not one
/// Aggregate's own behavior.
///
/// This document's own eight-step Permission Evaluation sequence is only partially
/// this type's responsibility. Steps 1-2 (Authentication Status, Identity Status)
/// are Identity Framework's own concern (authorization-framework.md's Scope section
/// explicitly excludes "User Authentication, Identity Management") -- the caller
/// must already have authenticated the principal and confirmed
/// <c>UserAccount.Status == Active</c> before reaching this service; reaching into
/// the Identity aggregate from here to re-check it would cross an aggregate boundary
/// this service does not own (aggregate-design-rules.md Rule 13). Steps 5-6
/// (Applicable Policies, Contextual Attributes -- full ABAC/policy evaluation) are a
/// deliberate Sprint 3 gap: this document's own Policy section describes evaluating
/// arbitrary conditions (time, device, network, security clearance...), which needs
/// the Rules Engine, built later in this same Sprint 3 and not yet available when
/// Authorization Framework is built (IMPLEMENTATION-PLAN.md's bootstrap order).
/// Steps 3-4 and 7-8 (Assigned Roles, Organizational Scope, Requested Resource,
/// Requested Action) are what this method actually evaluates today via RBAC plus
/// scope. Wire in Applicable Policies once the Rules Engine exists, per the Sprint's
/// own "wired in incrementally" resolution.
///
/// Default is always deny (`CTR-AUT-002`): every early return below is a denial, and
/// the single allow path is reached only after every check passes.
/// </summary>
public sealed class AuthorizationEvaluator
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IRolePermissionGrantRepository _rolePermissionGrantRepository;

    public AuthorizationEvaluator(
        IRoleAssignmentRepository roleAssignmentRepository,
        IRolePermissionGrantRepository rolePermissionGrantRepository)
    {
        _roleAssignmentRepository = Guard.AgainstNull(roleAssignmentRepository, nameof(roleAssignmentRepository));
        _rolePermissionGrantRepository = Guard.AgainstNull(rolePermissionGrantRepository, nameof(rolePermissionGrantRepository));
    }

    public async Task<AuthorizationDecision> EvaluateAsync(
        UserAccountId principalId,
        PermissionKey requestedPermission,
        OrganizationalScope resourceScope,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        Guard.AgainstNull(requestedPermission, nameof(requestedPermission));
        Guard.AgainstNull(resourceScope, nameof(resourceScope));

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        var allAssignments = await _roleAssignmentRepository
            .GetByPrincipalAsync(principalId, cancellationToken)
            .ConfigureAwait(false);

        var effectiveAssignments = allAssignments.Where(a => a.IsEffective(today)).ToList();
        if (effectiveAssignments.Count == 0)
        {
            return Deny("The principal holds no effective role assignment.");
        }

        var effectiveRoles = effectiveAssignments.Select(a => a.Role).Distinct().ToList();

        var grants = await _rolePermissionGrantRepository
            .GetActiveGrantsForRolesAsync(effectiveRoles, cancellationToken)
            .ConfigureAwait(false);

        var rolesHoldingPermission = grants
            .Where(g => g.IsActive && g.Permission.Equals(requestedPermission))
            .Select(g => g.Role)
            .ToHashSet();

        if (rolesHoldingPermission.Count == 0)
        {
            return Deny($"No effective role grants '{requestedPermission}'.");
        }

        var inScopeAssignment = effectiveAssignments.FirstOrDefault(a =>
            rolesHoldingPermission.Contains(a.Role) && a.Scope.Covers(resourceScope));

        return inScopeAssignment is null
            ? Deny($"A role granting '{requestedPermission}' is held, but not at the requested scope ({resourceScope}).")
            : AuthorizationDecision.Allow(principalId, requestedPermission, nowUtc);

        AuthorizationDecision Deny(string reason) => AuthorizationDecision.Deny(principalId, requestedPermission, reason, nowUtc);
    }
}
