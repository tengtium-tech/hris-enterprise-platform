using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Application.Dtos;
using Hris.Foundation.JobProcessing.Application.Mapping;
using Hris.Foundation.JobProcessing.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.JobProcessing.Application.Queries;

/// <summary>
/// Reads one job queue back by its own natural key -- the queue name a caller
/// submitting a job actually has in hand, matching <c>GetNumberSeriesQuery</c>'s own
/// identical by-natural-key shape.
/// </summary>
public sealed record GetJobQueueQuery(string Name) : IQuery<Result<JobQueueDto>>;

internal sealed class GetJobQueueQueryHandler : IRequestHandler<GetJobQueueQuery, Result<JobQueueDto>>
{
    private readonly IJobQueueRepository _repository;

    public GetJobQueueQueryHandler(IJobQueueRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<JobQueueDto>> Handle(GetJobQueueQuery request, CancellationToken cancellationToken)
    {
        var nameResult = JobQueueName.Create(request.Name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<JobQueueDto>(nameResult.Error);
        }

        var jobQueue = await _repository.GetByNameAsync(nameResult.Value, cancellationToken).ConfigureAwait(false);

        return jobQueue is null
            ? Result.Failure<JobQueueDto>(JobProcessingErrors.JobQueueNotFound)
            : Result.Success(JobProcessingMapper.ToDto(jobQueue));
    }
}
