using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Identity of the <see cref="EmployeeHistory"/> Aggregate Root. Source:
/// docs/04-modules/employee/domain/aggregates.md's Employee History Aggregate
/// section and employee-history.md.
/// </summary>
public readonly record struct EmployeeHistoryId(Guid Value) : IStronglyTypedId;
