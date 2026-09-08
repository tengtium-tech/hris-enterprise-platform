using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Identity of the <see cref="Employee"/> Aggregate Root. Source:
/// docs/04-modules/employee/domain/entities.md, Employee Aggregate Root Identity
/// ("EmployeeId"). Distinct from the tenant-facing Employee Number
/// (<see cref="EmployeeNumber"/>) -- see employee-numbering.md's own "Identifier
/// Types" table ("the two identifiers serve a distinct business purpose").
/// </summary>
public readonly record struct EmployeeId(Guid Value) : IStronglyTypedId;
