namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Repository contract for the <see cref="Employment"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split. <see cref="GetByIdAsync"/> deliberately does not take a
/// tenant identifier, the same established pattern <c>IPositionRepository</c>
/// already follows: the calling Application-layer handler verifies the loaded
/// aggregate's own <see cref="Employment.TenantId"/>.
///
/// <see cref="ExistsWithNumberAsync"/> and <see cref="GetActivePrimaryEmploymentAsync"/>
/// exist because tenant-wide Employment Number uniqueness (employment-numbering.md)
/// and "only one active primary Employment per Employee" (EMP-004) both reach across
/// Aggregate instances, something no single loaded Aggregate can check about itself.
/// </summary>
public interface IEmploymentRepository
{
    Task<Employment?> GetByIdAsync(EmploymentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Employment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Employment>> ListByEmployeeIdAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNumberAsync(
        Guid tenantId, string number, EmploymentId? excludeId, CancellationToken cancellationToken);

    Task<Employment?> GetActivePrimaryEmploymentAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken);

    Task AddAsync(Employment employment, CancellationToken cancellationToken);
}
