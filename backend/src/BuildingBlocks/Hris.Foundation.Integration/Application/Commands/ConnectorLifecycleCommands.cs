using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Commands;

/// <summary>
/// The three Connector lifecycle transitions -- Activate, Suspend, Deactivate --
/// grouped into one file the same way every other framework's own bundled lifecycle
/// commands are (see <c>FileLifecycleCommands</c>/<c>DocumentLifecycleCommands</c>).
/// Each handler is the same shape: load the aggregate by id (tenant-checked via
/// <see cref="IntegrationLookup"/>), call the one Domain method, and return its own
/// <see cref="Result"/>.
/// </summary>
public sealed record ActivateConnectorCommand(Guid ConnectorId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateConnectorCommandHandler : IRequestHandler<ActivateConnectorCommand, Result>
{
    private readonly IConnectorRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateConnectorCommandHandler(IConnectorRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateConnectorCommand request, CancellationToken cancellationToken)
    {
        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _repository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return connectorResult.IsFailure ? Result.Failure(connectorResult.Error) : connectorResult.Value.Activate(_timeProvider.GetUtcNow());
    }
}

public sealed record SuspendConnectorCommand(Guid ConnectorId, Guid TenantId) : ICommand<Result>;

internal sealed class SuspendConnectorCommandHandler : IRequestHandler<SuspendConnectorCommand, Result>
{
    private readonly IConnectorRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SuspendConnectorCommandHandler(IConnectorRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SuspendConnectorCommand request, CancellationToken cancellationToken)
    {
        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _repository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return connectorResult.IsFailure ? Result.Failure(connectorResult.Error) : connectorResult.Value.Suspend(_timeProvider.GetUtcNow());
    }
}

public sealed record DeactivateConnectorCommand(Guid ConnectorId, Guid TenantId) : ICommand<Result>;

internal sealed class DeactivateConnectorCommandHandler : IRequestHandler<DeactivateConnectorCommand, Result>
{
    private readonly IConnectorRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeactivateConnectorCommandHandler(IConnectorRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeactivateConnectorCommand request, CancellationToken cancellationToken)
    {
        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _repository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return connectorResult.IsFailure
            ? Result.Failure(connectorResult.Error)
            : connectorResult.Value.Deactivate(_timeProvider.GetUtcNow());
    }
}
