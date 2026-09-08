using Hris.Application.Abstractions;
using Hris.Modules.Position.Application.Dtos;
using Hris.Modules.Position.Application.Mapping;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Queries;

public sealed record GetJobClassificationQuery(Guid JobClassificationId, Guid TenantId) : IQuery<Result<JobClassificationDto>>;

internal sealed class GetJobClassificationQueryHandler : IRequestHandler<GetJobClassificationQuery, Result<JobClassificationDto>>
{
    private readonly IJobClassificationRepository _repository;

    public GetJobClassificationQueryHandler(IJobClassificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<JobClassificationDto>> Handle(GetJobClassificationQuery request, CancellationToken cancellationToken)
    {
        var jobClassificationResult = await PositionLookup.LoadJobClassificationForTenantAsync(
            _repository, request.JobClassificationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobClassificationResult.IsFailure
            ? Result.Failure<JobClassificationDto>(jobClassificationResult.Error)
            : Result.Success(PositionMapper.ToDto(jobClassificationResult.Value));
    }
}

public sealed record ListJobClassificationsQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<JobClassificationSummaryDto>>>;

internal sealed class ListJobClassificationsQueryHandler
    : IRequestHandler<ListJobClassificationsQuery, Result<IReadOnlyList<JobClassificationSummaryDto>>>
{
    private readonly IJobClassificationRepository _repository;

    public ListJobClassificationsQueryHandler(IJobClassificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<JobClassificationSummaryDto>>> Handle(
        ListJobClassificationsQuery request, CancellationToken cancellationToken)
    {
        var jobClassifications = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<JobClassificationSummaryDto> dtos = jobClassifications.Select(PositionMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
