using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Application.Dtos;
using Hris.Foundation.JobProcessing.Application.Mapping;
using Hris.Foundation.JobProcessing.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.JobProcessing.Application.Queries;

public sealed record GetJobQuery(Guid JobId) : IQuery<Result<JobDto>>;

internal sealed class GetJobQueryHandler : IRequestHandler<GetJobQuery, Result<JobDto>>
{
    private readonly IJobRepository _repository;

    public GetJobQueryHandler(IJobRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<JobDto>> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);

        return job is null
            ? Result.Failure<JobDto>(JobProcessingErrors.JobNotFound)
            : Result.Success(JobProcessingMapper.ToDto(job));
    }
}
