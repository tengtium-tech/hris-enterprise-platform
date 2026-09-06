using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Commands;

/// <summary>
/// Adds and removes a <see cref="Connector"/>'s own <see cref="DataMapping"/> child
/// entities, per integration-framework.md's own Data Mapping section.
/// </summary>
public sealed record AddDataMappingCommand(
    Guid ConnectorId, Guid TenantId, string SourceField, string TargetField, string TransformationType, string? TransformationRule)
    : ICommand<Result<Guid>>;

internal sealed class AddDataMappingCommandHandler : IRequestHandler<AddDataMappingCommand, Result<Guid>>
{
    private readonly IConnectorRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AddDataMappingCommandHandler(IConnectorRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AddDataMappingCommand request, CancellationToken cancellationToken)
    {
        var transformationTypeResult = IntegrationMapper.ParseTransformationType(request.TransformationType);
        if (transformationTypeResult.IsFailure)
        {
            return Result.Failure<Guid>(transformationTypeResult.Error);
        }

        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _repository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (connectorResult.IsFailure)
        {
            return Result.Failure<Guid>(connectorResult.Error);
        }

        return connectorResult.Value.AddDataMapping(
            request.SourceField, request.TargetField, transformationTypeResult.Value, request.TransformationRule,
            _timeProvider.GetUtcNow());
    }
}

public sealed record RemoveDataMappingCommand(Guid ConnectorId, Guid TenantId, Guid DataMappingId) : ICommand<Result>;

internal sealed class RemoveDataMappingCommandHandler : IRequestHandler<RemoveDataMappingCommand, Result>
{
    private readonly IConnectorRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveDataMappingCommandHandler(IConnectorRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveDataMappingCommand request, CancellationToken cancellationToken)
    {
        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _repository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (connectorResult.IsFailure)
        {
            return Result.Failure(connectorResult.Error);
        }

        return connectorResult.Value.RemoveDataMapping(request.DataMappingId, _timeProvider.GetUtcNow());
    }
}
