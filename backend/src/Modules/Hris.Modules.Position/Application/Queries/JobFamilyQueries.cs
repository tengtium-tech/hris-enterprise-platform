using Hris.Application.Abstractions;
using Hris.Modules.Position.Application.Dtos;
using Hris.Modules.Position.Application.Mapping;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Queries;

public sealed record GetJobFamilyQuery(Guid JobFamilyId, Guid TenantId) : IQuery<Result<JobFamilyDto>>;

internal sealed class GetJobFamilyQueryHandler : IRequestHandler<GetJobFamilyQuery, Result<JobFamilyDto>>
{
    private readonly IJobFamilyRepository _repository;

    public GetJobFamilyQueryHandler(IJobFamilyRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<JobFamilyDto>> Handle(GetJobFamilyQuery request, CancellationToken cancellationToken)
    {
        var jobFamilyResult = await PositionLookup.LoadJobFamilyForTenantAsync(
            _repository, request.JobFamilyId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobFamilyResult.IsFailure
            ? Result.Failure<JobFamilyDto>(jobFamilyResult.Error)
            : Result.Success(PositionMapper.ToDto(jobFamilyResult.Value));
    }
}

public sealed record ListJobFamiliesQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<JobFamilySummaryDto>>>;

internal sealed class ListJobFamiliesQueryHandler
    : IRequestHandler<ListJobFamiliesQuery, Result<IReadOnlyList<JobFamilySummaryDto>>>
{
    private readonly IJobFamilyRepository _repository;

    public ListJobFamiliesQueryHandler(IJobFamilyRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<JobFamilySummaryDto>>> Handle(
        ListJobFamiliesQuery request, CancellationToken cancellationToken)
    {
        var jobFamilies = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<JobFamilySummaryDto> dtos = jobFamilies.Select(PositionMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
