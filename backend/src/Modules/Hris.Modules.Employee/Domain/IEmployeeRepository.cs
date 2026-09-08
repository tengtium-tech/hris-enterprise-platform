namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Repository contract for the <see cref="Employee"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split. <see cref="GetByIdAsync"/> deliberately does not take a
/// tenant identifier, the same established pattern <c>IEmploymentRepository</c>
/// already follows: the calling Application-layer handler verifies the loaded
/// aggregate's own <see cref="Employee.TenantId"/>.
///
/// <see cref="ExistsWithNumberAsync"/> exists because tenant-wide Employee Number
/// uniqueness (BR-EMP-002) reaches across Aggregate instances, something no single
/// loaded Aggregate can check about itself.
/// </summary>
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Employee>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNumberAsync(Guid tenantId, string number, EmployeeId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(Employee employee, CancellationToken cancellationToken);
}
