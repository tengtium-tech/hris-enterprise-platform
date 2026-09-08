using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Identity of an <see cref="EmergencyContact"/> child entity. Source:
/// docs/04-modules/employee/domain/entities.md's EmergencyContact section.
/// </summary>
public readonly record struct EmergencyContactId(Guid Value) : IStronglyTypedId;
