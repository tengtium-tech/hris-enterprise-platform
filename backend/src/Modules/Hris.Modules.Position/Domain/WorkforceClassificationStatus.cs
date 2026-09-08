namespace Hris.Modules.Position.Domain;

/// <summary>
/// Shared lifecycle status for the three workforce classification Aggregate Roots:
/// <see cref="JobFamily"/>, <see cref="JobClassification"/>, and <see cref="JobGrade"/>.
/// Source: job-families.md, job-classifications.md, and job-grades.md, each of which
/// shows the identical three-state Created-&gt;Active-&gt;Inactive-&gt;Archived
/// lifecycle diagram (the "Created" arrow enters Active directly; it is a transition,
/// not a held state, unlike <see cref="Position"/>'s own distinct Draft state). One
/// shared enum rather than three duplicated ones, the same consolidation
/// OrganizationalUnitStatus already applies across Organization's own six child
/// entity types.
/// </summary>
public enum WorkforceClassificationStatus
{
    Active = 0,
    Inactive = 1,
    Archived = 2,
}
