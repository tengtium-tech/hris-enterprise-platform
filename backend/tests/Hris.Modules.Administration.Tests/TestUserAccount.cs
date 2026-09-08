using Hris.Modules.Administration.Domain;

namespace Hris.Modules.Administration.Tests;

/// <summary>
/// Shared aggregate-construction helper for Domain and Application-layer handler
/// tests, the same role <c>TestEmployee</c> plays for
/// <c>Hris.Modules.Employee.Tests</c>.
/// </summary>
internal static class TestUserAccount
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static UserAccount CreateEmployeeLinked(Guid tenantId, Guid? employeeId = null) =>
        UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), tenantId, AccountType.EmployeeLinked, employeeId ?? Guid.NewGuid(), null, null,
            Guid.NewGuid(), NowUtc).Value;

    public static UserAccount CreateActiveEmployeeLinked(Guid tenantId, Guid? employeeId = null)
    {
        var account = CreateEmployeeLinked(tenantId, employeeId);
        account.Activate(NowUtc);
        return account;
    }

    public static UserAccount CreateActiveTenantAdministrator(Guid tenantId)
    {
        var account = CreateActiveEmployeeLinked(tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.SystemAdministrator);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(role, scope, DateOnly.FromDateTime(NowUtc.UtcDateTime), null, Guid.NewGuid(), "Initial tenant administrator", null, true, NowUtc);
        return account;
    }
}
