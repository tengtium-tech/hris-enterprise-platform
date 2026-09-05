using Hris.Application.Abstractions;
using Hris.Foundation.Entitlement.Application.Dtos;
using Hris.Foundation.Entitlement.Application.Mapping;
using Hris.Foundation.Entitlement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Entitlement.Application.Queries;

/// <summary>
/// Returns the full Process Pack catalogue -- entitlement-framework.md's own Pack
/// Catalog section, as data rather than as a document a caller has to parse.
/// </summary>
public sealed record ListProcessPacksQuery : IQuery<Result<IReadOnlyCollection<ProcessPackDto>>>;

internal sealed class ListProcessPacksQueryHandler : IRequestHandler<ListProcessPacksQuery, Result<IReadOnlyCollection<ProcessPackDto>>>
{
    public Task<Result<IReadOnlyCollection<ProcessPackDto>>> Handle(ListProcessPacksQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProcessPackDto> packs = ProcessPackCatalog.AllPacks.Select(pack => pack.ToDto()).ToList();

        return Task.FromResult(Result.Success(packs));
    }
}
