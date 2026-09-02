using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using DomainRoleAssignment = Hris.Foundation.Authorization.Domain.RoleAssignment;

namespace Hris.Foundation.Authorization.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same
/// document's own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly DateOnly Today = DateOnly.FromDateTime(NowUtc.UtcDateTime);

    public static UserAccountId NewPrincipalId() => new(Guid.NewGuid());

    public static OrganizationalScope Scope(
        OrganizationalScopeLevel level = OrganizationalScopeLevel.Tenant,
        Guid? scopeId = null) =>
        OrganizationalScope.Create(level, scopeId ?? Guid.NewGuid()).Value;

    public static RoleAssignment RoleAssignment(
        Role role = Role.HRManager,
        OrganizationalScope? scope = null,
        DateOnly? effectiveDate = null,
        DateOnly? expirationDate = null,
        RoleAssignmentType assignmentType = RoleAssignmentType.Direct,
        UserAccountId? principalId = null,
        UserAccountId? grantedByPrincipalId = null,
        DateTimeOffset? nowUtc = null) =>
        DomainRoleAssignment.Create(
            principalId ?? NewPrincipalId(),
            role,
            scope ?? Scope(),
            assignmentType,
            effectiveDate ?? Today,
            expirationDate,
            grantedByPrincipalId ?? NewPrincipalId(),
            nowUtc ?? NowUtc).Value;

    public static PermissionKey Permission(
        string resourceType = "Employee",
        PermissionAction action = PermissionAction.Read) =>
        PermissionKey.Create(resourceType, action).Value;

    public static RolePermissionGrant Grant(
        Role role = Role.HRManager,
        PermissionKey? permission = null,
        DateTimeOffset? nowUtc = null) =>
        RolePermissionGrant.Create(role, permission ?? Permission(), nowUtc ?? NowUtc).Value;
}
