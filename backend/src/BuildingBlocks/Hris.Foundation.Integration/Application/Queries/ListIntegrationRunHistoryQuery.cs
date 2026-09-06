using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Dtos;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Queries;

/// <summary>
/// integration-framework.md's own Monitoring section ("Successful Integrations,
/// Failed Integrations, Retry Count"). <paramref name="TenantId"/> is mandatory, the
/// same shape <c>ListJobHistoryQuery</c>'s own remarks establish for the identical
/// "run history for one owner" need (<c>CTR-ISO-004</c>).
/// </summary>
public sealed record ListIntegrationRunHistoryQuery(Guid TenantId, Guid ConnectorId) : IQuery<Result<IReadOnlyList<IntegrationRunDto>>>;

internal sealed class ListIntegrationRunHistoryQueryHandler
    : IRequestHandler<ListIntegrationRunHistoryQuery, Result<IReadOnlyList<IntegrationRunDto>>>
{
    private const int _maxResults = 100;

    private readonly IIntegrationRunRepository _repository;

    public ListIntegrationRunHistoryQueryHandler(IIntegrationRunRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<IntegrationRunDto>>> Handle(
        ListIntegrationRunHistoryQuery request, CancellationToken cancellationToken)
    {
        var runs = await _repository
            .ListHistoryAsync(request.TenantId, new ConnectorId(request.ConnectorId), _maxResults, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<IntegrationRunDto> dtos = runs.Select(IntegrationMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
