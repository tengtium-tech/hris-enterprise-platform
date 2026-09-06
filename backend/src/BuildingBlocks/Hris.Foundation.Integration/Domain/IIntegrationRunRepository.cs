namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Repository contract for the <see cref="IntegrationRun"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. <see cref="ListHistoryAsync"/> backs this framework's own
/// Monitoring section ("Successful Integrations, Failed Integrations, Retry Count"),
/// the same shape <c>IJobRepository.ListByQueueAsync</c> already establishes for the
/// identical "run history for one owner" need.
/// </summary>
public interface IIntegrationRunRepository
{
    Task<IntegrationRun?> GetByIdAsync(IntegrationRunId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<IntegrationRun>> ListHistoryAsync(
        Guid tenantId, ConnectorId connectorId, int maxResults, CancellationToken cancellationToken);

    Task AddAsync(IntegrationRun integrationRun, CancellationToken cancellationToken);
}
