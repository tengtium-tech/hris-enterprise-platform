namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Shared lifecycle status for the Organization Aggregate and every child entity in
/// its own hierarchy (BusinessUnit, Division, Department, Section, Team,
/// CostCenter), plus the WorkLocation and LegalEntity Aggregate Roots.
///
/// domain/entities.md's own Entity Lifecycle names four stages (Create, Rename,
/// Restructure, Archive) but those are operations, not states; the only persisted
/// state those operations actually move between is Active and Archived, with
/// Restore returning to Active. domain/locations.md and domain/legal-entities.md
/// each separately show a four-stage Created/Active/Inactive/Archived lifecycle
/// diagram, but application/commands.md defines no Activate/Deactivate command for
/// either aggregate, only Create/Update/Archive/Restore, and business-rules.md's own
/// concrete rules (LOC-002, LEG-003, and so on) only ever reference "archived," never
/// "inactive." The two-state model here follows the more concrete, rule- and
/// command-driven specification over the narrative overview diagrams.
/// </summary>
public enum OrganizationalUnitStatus
{
    Active = 0,
    Archived = 1,
}
