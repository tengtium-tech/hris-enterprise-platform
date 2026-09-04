using Hris.Application.Abstractions;
using Hris.Foundation.StatutoryReferenceData.Application.Dtos;
using Hris.Foundation.StatutoryReferenceData.Application.Mapping;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.StatutoryReferenceData.Application.Queries;

public sealed record ListStatutoryTableVersionHistoryQuery(
    Guid StatutoryProgramId) : IQuery<Result<IReadOnlyList<StatutoryTableVersionDto>>>;

internal sealed class ListStatutoryTableVersionHistoryQueryHandler
    : IRequestHandler<ListStatutoryTableVersionHistoryQuery, Result<IReadOnlyList<StatutoryTableVersionDto>>>
{
    private readonly IStatutoryTableVersionRepository _repository;

    public ListStatutoryTableVersionHistoryQueryHandler(IStatutoryTableVersionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<StatutoryTableVersionDto>>> Handle(
        ListStatutoryTableVersionHistoryQuery request, CancellationToken cancellationToken)
    {
        var versions = await _repository.ListByProgramAsync(
            new StatutoryProgramId(request.StatutoryProgramId), cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<StatutoryTableVersionDto>>(
            versions.Select(StatutoryReferenceDataMapper.ToDto).ToList());
    }
}
