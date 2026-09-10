using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Application.Dtos;
using Hris.Modules.Timekeeping.Application.Mapping;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Queries;

public sealed record ListShiftAssignmentsQuery(Guid TenantId, string? TargetId)
    : IQuery<Result<IReadOnlyList<ShiftAssignmentDto>>>;

internal sealed class ListShiftAssignmentsQueryHandler
    : IRequestHandler<ListShiftAssignmentsQuery, Result<IReadOnlyList<ShiftAssignmentDto>>>
{
    private readonly IShiftAssignmentRepository _repository;

    public ListShiftAssignmentsQueryHandler(IShiftAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<ShiftAssignmentDto>>> Handle(
        ListShiftAssignmentsQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var assignments = string.IsNullOrWhiteSpace(request.TargetId)
            ? await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false)
            : await _repository.ListByTargetAsync(request.TenantId, request.TargetId.Trim(), cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyList<ShiftAssignmentDto> dtos = assignments.Select(TimekeepingMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
