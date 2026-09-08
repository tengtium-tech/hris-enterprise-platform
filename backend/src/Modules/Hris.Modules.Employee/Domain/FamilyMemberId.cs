using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Identity of a <see cref="FamilyMember"/> child entity. Source:
/// docs/04-modules/employee/domain/entities.md's FamilyMember section.
/// </summary>
public readonly record struct FamilyMemberId(Guid Value) : IStronglyTypedId;
