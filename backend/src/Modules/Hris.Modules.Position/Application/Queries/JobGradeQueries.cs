using Hris.Application.Abstractions;
using Hris.Modules.Position.Application.Dtos;
using Hris.Modules.Position.Application.Mapping;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Queries;

public sealed record GetJobGradeQuery(Guid JobGradeId, Guid TenantId) : IQuery<Result<JobGradeDto>>;

internal sealed class GetJobGradeQueryHandler : IRequestHandler<GetJobGradeQuery, Result<JobGradeDto>>
{
    private readonly IJobGradeRepository _repository;

    public GetJobGradeQueryHandler(IJobGradeRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<JobGradeDto>> Handle(GetJobGradeQuery request, CancellationToken cancellationToken)
    {
        var jobGradeResult = await PositionLookup.LoadJobGradeForTenantAsync(
            _repository, request.JobGradeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobGradeResult.IsFailure
            ? Result.Failure<JobGradeDto>(jobGradeResult.Error)
            : Result.Success(PositionMapper.ToDto(jobGradeResult.Value));
    }
}

public sealed record ListJobGradesQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<JobGradeSummaryDto>>>;

internal sealed class ListJobGradesQueryHandler
    : IRequestHandler<ListJobGradesQuery, Result<IReadOnlyList<JobGradeSummaryDto>>>
{
    private readonly IJobGradeRepository _repository;

    public ListJobGradesQueryHandler(IJobGradeRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<JobGradeSummaryDto>>> Handle(
        ListJobGradesQuery request, CancellationToken cancellationToken)
    {
        var jobGrades = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<JobGradeSummaryDto> dtos = jobGrades.Select(PositionMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
