namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Repository contract for the <see cref="Connector"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. <see cref="GetByIdAsync"/> deliberately does not take a
/// tenant identifier -- per this codebase's own established pattern (see, for example,
/// <c>IJobRepository.GetByIdAsync</c>), a repository loads by surrogate key alone, and
/// the calling Application-layer handler verifies the loaded aggregate's own
/// <see cref="Connector.TenantId"/> before returning anything, per CTR-ISO-004.
/// </summary>
public interface IConnectorRepository
{
    Task<Connector?> GetByIdAsync(ConnectorId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Connector>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(Connector connector, CancellationToken cancellationToken);
}
