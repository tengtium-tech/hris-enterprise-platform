namespace Hris.Modules.Employment.Domain;

/// <summary>
/// How and where the employee performs work under an Employment Assignment. Source:
/// docs/04-modules/employment/domain/value-objects.md, WorkArrangement -- "owned by
/// the Employment Assignment", confirmed by entities.md's own Entity Usage table
/// ("EmploymentAssignment | EmploymentAssignmentId, WorkArrangement, EffectiveDate").
/// Not an Employment-level property, despite appearing in value-objects.md's
/// document alongside Employment's own Value Objects.
/// </summary>
public enum WorkArrangement
{
    OnSite = 0,
    Remote = 1,
    Hybrid = 2,
    FieldBased = 3,
    HomeBased = 4,
}
