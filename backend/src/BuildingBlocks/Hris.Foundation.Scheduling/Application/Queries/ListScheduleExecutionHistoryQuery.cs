using Hris.Application.Abstractions;
using Hris.Foundation.Scheduling.Application.Dtos;
using Hris.Foundation.Scheduling.Application.Mapping;
using Hris.Foundation.Scheduling.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Scheduling.Application.Queries;

/// <summary>
/// scheduling-framework.md's own Schedule History section: "Schedule Identifier,
/// Execution Time, Trigger Time, Job Identifier, Result, Duration, Failure Reason,
/// Retry Information... should support auditing and troubleshooting."
/// <paramref name="TenantId"/> is mandatory, per <see cref="IScheduleExecutionRepository.ListByScheduleAsync"/>'s
/// own remarks (<c>CTR-ISO-004</c>).
/// </summary>
public sealed record ListScheduleExecutionHistoryQuery(Guid ScheduleId, Guid TenantId) : IQuery<Result<IReadOnlyList<ScheduleExecutionDto>>>;

internal sealed class ListScheduleExecutionHistoryQueryHandler
    : IRequestHandler<ListScheduleExecutionHistoryQuery, Result<IReadOnlyList<ScheduleExecutionDto>>>
{
    private const int _maxResults = 100;

    private readonly IScheduleExecutionRepository _repository;

    public ListScheduleExecutionHistoryQueryHandler(IScheduleExecutionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<ScheduleExecutionDto>>> Handle(
        ListScheduleExecutionHistoryQuery request, CancellationToken cancellationToken)
    {
        var executions = await _repository
            .ListByScheduleAsync(new ScheduleId(request.ScheduleId), request.TenantId, _maxResults, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ScheduleExecutionDto> dtos = executions.Select(SchedulingMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
