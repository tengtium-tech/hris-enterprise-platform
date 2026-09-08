using Hris.Application.Abstractions;
using Hris.Modules.Employment.Application.Dtos;
using Hris.Modules.Employment.Application.Mapping;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Queries;

public sealed record GetEmploymentQuery(Guid EmploymentId, Guid TenantId) : IQuery<Result<EmploymentDto>>;

internal sealed class GetEmploymentQueryHandler : IRequestHandler<GetEmploymentQuery, Result<EmploymentDto>>
{
    private readonly IEmploymentRepository _repository;

    public GetEmploymentQueryHandler(IEmploymentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<EmploymentDto>> Handle(GetEmploymentQuery request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure<EmploymentDto>(employmentResult.Error)
            : Result.Success(EmploymentMapper.ToDto(employmentResult.Value));
    }
}

/// <summary>
/// Lists Employments for a tenant, optionally narrowed by
/// <see cref="EmploymentLifecycleStage"/> and/or the owning Employee -- one general
/// query rather than a separate query class per filter combination, the same
/// "avoid manufacturing near-duplicate query types" simplification
/// <c>ListPositionsQuery</c> already applies.
/// </summary>
public sealed record ListEmploymentsQuery(
    Guid TenantId, Guid? EmployeeId, EmploymentLifecycleStage? LifecycleStageFilter)
    : IQuery<Result<IReadOnlyList<EmploymentSummaryDto>>>;

internal sealed class ListEmploymentsQueryHandler
    : IRequestHandler<ListEmploymentsQuery, Result<IReadOnlyList<EmploymentSummaryDto>>>
{
    private readonly IEmploymentRepository _repository;

    public ListEmploymentsQueryHandler(IEmploymentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<EmploymentSummaryDto>>> Handle(
        ListEmploymentsQuery request, CancellationToken cancellationToken)
    {
        var employments = request.EmployeeId.HasValue
            ? await _repository.ListByEmployeeIdAsync(request.TenantId, request.EmployeeId.Value, cancellationToken)
                .ConfigureAwait(false)
            : await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IEnumerable<Domain.Employment> filtered = employments;
        if (request.LifecycleStageFilter.HasValue)
        {
            filtered = filtered.Where(employment => employment.LifecycleStage == request.LifecycleStageFilter.Value);
        }

        IReadOnlyList<EmploymentSummaryDto> dtos = filtered.Select(EmploymentMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
