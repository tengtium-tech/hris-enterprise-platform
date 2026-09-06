using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Commands;

public sealed record UpdateConnectorEndpointCommand(Guid ConnectorId, Guid TenantId, string EndpointType, string EndpointAddress)
    : ICommand<Result>;

internal sealed class UpdateConnectorEndpointCommandHandler : IRequestHandler<UpdateConnectorEndpointCommand, Result>
{
    private readonly IConnectorRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateConnectorEndpointCommandHandler(IConnectorRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateConnectorEndpointCommand request, CancellationToken cancellationToken)
    {
        var endpointTypeResult = IntegrationMapper.ParseEndpointType(request.EndpointType);
        if (endpointTypeResult.IsFailure)
        {
            return Result.Failure(endpointTypeResult.Error);
        }

        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _repository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (connectorResult.IsFailure)
        {
            return Result.Failure(connectorResult.Error);
        }

        return connectorResult.Value.UpdateEndpoint(endpointTypeResult.Value, request.EndpointAddress, _timeProvider.GetUtcNow());
    }
}
