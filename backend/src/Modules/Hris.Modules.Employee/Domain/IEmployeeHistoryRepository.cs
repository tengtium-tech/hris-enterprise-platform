namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Repository contract for the <see cref="EmployeeHistory"/> Aggregate Root.
/// Append-only, per employee-history.md ("History records are immutable after
/// creation") -- there is deliberately no update or delete method.
/// </summary>
public interface IEmployeeHistoryRepository
{
    Task<IReadOnlyList<EmployeeHistory>> ListByEmployeeIdAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken);

    Task AddAsync(EmployeeHistory history, CancellationToken cancellationToken);
}
