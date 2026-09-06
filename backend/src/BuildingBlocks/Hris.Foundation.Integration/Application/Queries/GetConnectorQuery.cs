using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Dtos;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Queries;

/// <summary>
/// Reads one connector back by its own identifier, tenant-checked via
/// <see cref="IntegrationLookup"/> per CTR-ISO-004.
/// </summary>
public sealed record GetConnectorQuery(Guid ConnectorId, Guid TenantId) : IQuery<Result<ConnectorDto>>;

internal sealed class GetConnectorQueryHandler : IRequestHandler<GetConnectorQuery, Result<ConnectorDto>>
{
    private readonly IConnectorRepository _repository;

    public GetConnectorQueryHandler(IConnectorRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<ConnectorDto>> Handle(GetConnectorQuery request, CancellationToken cancellationToken)
    {
        var connectorResult = await IntegrationLookup.LoadConnectorForTenantAsync(
            _repository, request.ConnectorId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return connectorResult.IsFailure
            ? Result.Failure<ConnectorDto>(connectorResult.Error)
            : Result.Success(IntegrationMapper.ToDto(connectorResult.Value));
    }
}
