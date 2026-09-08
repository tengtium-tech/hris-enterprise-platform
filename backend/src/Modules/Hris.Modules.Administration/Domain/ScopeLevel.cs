namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Source: docs/04-modules/administration/domain/role-assignments.md's "Scope
/// Levels" table. Ordered broadest to narrowest for AR-002's "no broader a scope
/// than held" comparison (<see cref="OrganizationalScope.IsBroaderThanOrEqualTo"/>)
/// -- <see cref="ReportingLine"/> and <see cref="Self"/> are relative scopes
/// resolved against the holder at evaluation time, not positioned within the
/// organizational-unit hierarchy, and are treated as narrower than every
/// organizational-unit level for that comparison, per role-assignments.md's own
/// "ReportingLine is not an organizational unit" distinction.
/// </summary>
public enum ScopeLevel
{
    Tenant = 0,
    LegalEntity = 1,
    BusinessUnit = 2,
    Department = 3,
    Team = 4,
    ReportingLine = 5,
    Self = 6,
}
