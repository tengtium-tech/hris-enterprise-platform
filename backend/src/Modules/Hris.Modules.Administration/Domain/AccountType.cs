namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Source: docs/04-modules/administration/domain/platform-users.md's "Account
/// Types" table. Immutable after provisioning (AR-016).
/// </summary>
public enum AccountType
{
    EmployeeLinked = 0,
    External = 1,
    Service = 2,
}
