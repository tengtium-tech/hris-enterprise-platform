using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// A member of the employee's family. Source:
/// docs/04-modules/employee/domain/entities.md's FamilyMember section
/// ("Relationship, Personal information, Dependency status... Dependency rules are
/// governed by Benefits policies" -- this module records the fact, not the
/// eligibility rule, matching employee-profile.md's "This information supports
/// benefits administration" without duplicating Benefits' own logic). A child
/// Entity of <see cref="Employee"/>, never an Aggregate Root of its own; its
/// constructor and mutators are <c>internal</c>.
/// </summary>
public sealed class FamilyMember : Entity<FamilyMemberId>
{
    public string Name { get; private set; }

    public FamilyRelationship Relationship { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    public bool IsDependent { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal FamilyMember(
        FamilyMemberId id, string name, FamilyRelationship relationship, DateOnly? dateOfBirth, bool isDependent,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        Relationship = relationship;
        DateOfBirth = dateOfBirth;
        IsDependent = isDependent;
        CreatedAtUtc = createdAtUtc;
    }

    internal void UpdateDetails(string name, FamilyRelationship relationship, DateOnly? dateOfBirth, bool isDependent)
    {
        Name = name;
        Relationship = relationship;
        DateOfBirth = dateOfBirth;
        IsDependent = isDependent;
    }
}
