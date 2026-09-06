using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Dtos;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Queries;

/// <summary>
/// Lists every connector registered for one tenant -- not paged: the number of
/// registered connectors per tenant is small (one per external system relationship),
/// unlike a business-record collection such as Document Management's own documents.
/// </summary>
public sealed record ListConnectorsQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<ConnectorSummaryDto>>>;

internal sealed class ListConnectorsQueryHandler : IRequestHandler<ListConnectorsQuery, Result<IReadOnlyList<ConnectorSummaryDto>>>
{
    private readonly IConnectorRepository _repository;

    public ListConnectorsQueryHandler(IConnectorRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<ConnectorSummaryDto>>> Handle(ListConnectorsQuery request, CancellationToken cancellationToken)
    {
        var connectors = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<ConnectorSummaryDto> dtos = connectors.Select(IntegrationMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
