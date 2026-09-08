using Hris.Application.Abstractions;
using Hris.Modules.Position.Application.Dtos;
using Hris.Modules.Position.Application.Mapping;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Queries;

public sealed record GetPositionQuery(Guid PositionId, Guid TenantId) : IQuery<Result<PositionDto>>;

internal sealed class GetPositionQueryHandler : IRequestHandler<GetPositionQuery, Result<PositionDto>>
{
    private readonly IPositionRepository _repository;

    public GetPositionQueryHandler(IPositionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<PositionDto>> Handle(GetPositionQuery request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure<PositionDto>(positionResult.Error)
            : Result.Success(PositionMapper.ToDto(positionResult.Value));
    }
}

/// <summary>
/// Lists Positions for a tenant, optionally narrowed by <see cref="PositionStatus"/>
/// and/or <see cref="Domain.VacancyStatus"/> -- one general query rather than a
/// separate "Get Vacant Positions"/"Get Active Positions"/"Get Archived Positions"
/// query class per queries.md's own illustrative list, the same "avoid manufacturing
/// near-duplicate query types" simplification the Organization module already
/// applied to its own list queries.
/// </summary>
public sealed record ListPositionsQuery(
    Guid TenantId, PositionStatus? StatusFilter, VacancyStatus? VacancyFilter) : IQuery<Result<IReadOnlyList<PositionSummaryDto>>>;

internal sealed class ListPositionsQueryHandler
    : IRequestHandler<ListPositionsQuery, Result<IReadOnlyList<PositionSummaryDto>>>
{
    private readonly IPositionRepository _repository;

    public ListPositionsQueryHandler(IPositionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<PositionSummaryDto>>> Handle(
        ListPositionsQuery request, CancellationToken cancellationToken)
    {
        var positions = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IEnumerable<Domain.Position> filtered = positions;
        if (request.StatusFilter.HasValue)
        {
            filtered = filtered.Where(position => position.Status == request.StatusFilter.Value);
        }

        if (request.VacancyFilter.HasValue)
        {
            filtered = filtered.Where(position => position.VacancyStatus == request.VacancyFilter.Value);
        }

        IReadOnlyList<PositionSummaryDto> dtos = filtered.Select(PositionMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
