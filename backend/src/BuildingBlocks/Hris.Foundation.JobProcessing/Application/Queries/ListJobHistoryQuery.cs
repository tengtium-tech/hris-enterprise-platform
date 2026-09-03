using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Application.Dtos;
using Hris.Foundation.JobProcessing.Application.Mapping;
using Hris.Foundation.JobProcessing.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.JobProcessing.Application.Queries;

/// <summary>
/// job-processing.md's own Job History section: "Job Identifier, Job Type, Queue,
/// Submitter, Submission Time, Start Time, Completion Time, Status, Retry Count,
/// Failure Reason... should be searchable and auditable."
/// <paramref name="TenantId"/> is mandatory, per <see cref="IJobRepository.ListByQueueAsync"/>'s
/// own remarks (<c>CTR-ISO-004</c>).
/// </summary>
public sealed record ListJobHistoryQuery(Guid JobQueueId, Guid TenantId) : IQuery<Result<IReadOnlyList<JobDto>>>;

internal sealed class ListJobHistoryQueryHandler : IRequestHandler<ListJobHistoryQuery, Result<IReadOnlyList<JobDto>>>
{
    private const int _maxResults = 100;

    private readonly IJobRepository _repository;

    public ListJobHistoryQueryHandler(IJobRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<JobDto>>> Handle(ListJobHistoryQuery request, CancellationToken cancellationToken)
    {
        var jobs = await _repository
            .ListByQueueAsync(new JobQueueId(request.JobQueueId), request.TenantId, _maxResults, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<JobDto> dtos = jobs.Select(JobProcessingMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
