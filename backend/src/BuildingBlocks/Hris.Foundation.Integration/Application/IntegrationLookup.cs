using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Application;

/// <summary>
/// The one place every command/query handler that loads a <see cref="Connector"/> or
/// <see cref="IntegrationRun"/> by its own identifier performs this framework's own
/// tenant-isolation check -- integration-framework.md's own AI Implementation
/// Guidance: "Carry tenant context through every inbound and outbound integration
/// (CTR-ISO-004)." Returning a not-found error for both a genuinely missing record
/// and one that exists but belongs to a different tenant is deliberate, mirroring
/// CTR-ISO-002's own "not-found, not... a permission error that confirms the record's
/// existence" reasoning even though this document's own guidance cites CTR-ISO-004
/// specifically. Factored out once, rather than repeated inline in every handler, per
/// this project's own "prefer structure over discipline" -- the same choice
/// <c>DocumentLookup</c> already makes for the identical shape.
/// </summary>
internal static class IntegrationLookup
{
    public static async Task<Result<Connector>> LoadConnectorForTenantAsync(
        IConnectorRepository repository, Guid connectorId, Guid tenantId, CancellationToken cancellationToken)
    {
        var connector = await repository.GetByIdAsync(new ConnectorId(connectorId), cancellationToken).ConfigureAwait(false);

        return connector is null || connector.TenantId != tenantId
            ? Result.Failure<Connector>(IntegrationErrors.ConnectorNotFound)
            : Result.Success(connector);
    }

    public static async Task<Result<IntegrationRun>> LoadIntegrationRunForTenantAsync(
        IIntegrationRunRepository repository, Guid integrationRunId, Guid tenantId, CancellationToken cancellationToken)
    {
        var run = await repository.GetByIdAsync(new IntegrationRunId(integrationRunId), cancellationToken).ConfigureAwait(false);

        return run is null || run.TenantId != tenantId
            ? Result.Failure<IntegrationRun>(IntegrationErrors.IntegrationRunNotFound)
            : Result.Success(run);
    }
}
