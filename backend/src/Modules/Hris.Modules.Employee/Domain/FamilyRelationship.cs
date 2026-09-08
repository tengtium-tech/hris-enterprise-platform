namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Source: docs/04-modules/employee/domain/entities.md's FamilyMember section
/// ("Typical Relationships: Spouse, Child, Parent, Guardian, Dependent").
/// </summary>
public enum FamilyRelationship
{
    Spouse = 0,
    Child = 1,
    Parent = 2,
    Guardian = 3,
    Dependent = 4,
}
