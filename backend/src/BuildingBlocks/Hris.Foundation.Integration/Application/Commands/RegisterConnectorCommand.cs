using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Commands;

/// <summary>
/// Registers a new connector, per integration-framework.md's own Core Concepts
/// ("A Connector encapsulates integration logic"). Carries raw primitives, not Domain
/// Value Objects, across the MediatR boundary -- the same choice every other
/// framework's own commands in this codebase already make.
/// </summary>
public sealed record RegisterConnectorCommand(
    Guid TenantId, string Name, string IntegrationCategory, string EndpointType, string EndpointAddress) : ICommand<Result<Guid>>;

internal sealed class RegisterConnectorCommandHandler : IRequestHandler<RegisterConnectorCommand, Result<Guid>>
{
    private readonly IConnectorRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterConnectorCommandHandler(IConnectorRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterConnectorCommand request, CancellationToken cancellationToken)
    {
        var endpointTypeResult = IntegrationMapper.ParseEndpointType(request.EndpointType);
        if (endpointTypeResult.IsFailure)
        {
            return Result.Failure<Guid>(endpointTypeResult.Error);
        }

        var connectorResult = Connector.Register(
            request.TenantId, request.Name, request.IntegrationCategory, endpointTypeResult.Value, request.EndpointAddress,
            _timeProvider.GetUtcNow());

        if (connectorResult.IsFailure)
        {
            return Result.Failure<Guid>(connectorResult.Error);
        }

        await _repository.AddAsync(connectorResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(connectorResult.Value.Id.Value);
    }
}
