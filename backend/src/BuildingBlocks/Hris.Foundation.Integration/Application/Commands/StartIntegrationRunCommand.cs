using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Commands;

/// <summary>
/// Begins a new run against an existing, same-tenant <see cref="Connector"/>. Per
/// this framework's own Scope ("Never place business logic in an integration
/// adapter") this command records only that a run began -- the actual REST/SOAP/file/
/// message-queue call the run represents is the caller's own concern, invoked before
/// or after this command as appropriate to the caller's own workflow.
/// </summary>
public sealed record StartIntegrationRunCommand(
    Guid TenantId, Guid ConnectorId, string RunKind, string? SyncModel, Guid? JobId, int MaxRetries) : ICommand<Result<Guid>>;

internal sealed class StartIntegrationRunCommandHandler : IRequestHandler<StartIntegrationRunCommand, Result<Guid>>
{
    private readonly IConnectorRepository _connectorRepository;
    private readonly IIntegrationRunRepository _runRepository;
    private readonly TimeProvider _timeProvider;

    public StartIntegrationRunCommandHandler(
        IConnectorRepository connectorRepository, IIntegrationRunRepository runRepository, TimeProvider timeProvider)
    {
        _connectorRepository = Guard.AgainstNull(connectorRepository, nameof(connectorRepository));
        _runRepository = Guard.AgainstNull(runRepository, nameof(runRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(StartIntegrationRunCommand request, CancellationToken cancellationToken)
    {
        var runKindResult = IntegrationMapper.ParseRunKind(request.RunKind);
        if (runKindResult.IsFailure)
        {
            return Result.Failure<Guid>(runKindResult.Error);
        }

        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _connectorRepository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (connectorResult.IsFailure)
        {
            return Result.Failure<Guid>(connectorResult.Error);
        }

        var runResult = IntegrationRun.Start(
            request.TenantId, connectorResult.Value.Id, runKindResult.Value, request.SyncModel, request.JobId, request.MaxRetries,
            _timeProvider.GetUtcNow());

        if (runResult.IsFailure)
        {
            return Result.Failure<Guid>(runResult.Error);
        }

        await _runRepository.AddAsync(runResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(runResult.Value.Id.Value);
    }
}
