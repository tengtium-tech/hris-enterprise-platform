using Hris.Application.Abstractions;
using Hris.Modules.Employment.Application.Dtos;
using Hris.Modules.Employment.Application.Mapping;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Queries;

public sealed record GetEmploymentAssignmentQuery(Guid EmploymentAssignmentId, Guid TenantId)
    : IQuery<Result<EmploymentAssignmentDto>>;

internal sealed class GetEmploymentAssignmentQueryHandler
    : IRequestHandler<GetEmploymentAssignmentQuery, Result<EmploymentAssignmentDto>>
{
    private readonly IEmploymentAssignmentRepository _repository;

    public GetEmploymentAssignmentQueryHandler(IEmploymentAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<EmploymentAssignmentDto>> Handle(
        GetEmploymentAssignmentQuery request, CancellationToken cancellationToken)
    {
        var assignmentResult = await EmploymentLookup.LoadAssignmentForTenantAsync(
            _repository, request.EmploymentAssignmentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return assignmentResult.IsFailure
            ? Result.Failure<EmploymentAssignmentDto>(assignmentResult.Error)
            : Result.Success(EmploymentMapper.ToDto(assignmentResult.Value));
    }
}

public sealed record GetCurrentEmploymentAssignmentQuery(Guid TenantId, Guid EmploymentId)
    : IQuery<Result<EmploymentAssignmentDto>>;

internal sealed class GetCurrentEmploymentAssignmentQueryHandler
    : IRequestHandler<GetCurrentEmploymentAssignmentQuery, Result<EmploymentAssignmentDto>>
{
    private readonly IEmploymentAssignmentRepository _repository;

    public GetCurrentEmploymentAssignmentQueryHandler(IEmploymentAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<EmploymentAssignmentDto>> Handle(
        GetCurrentEmploymentAssignmentQuery request, CancellationToken cancellationToken)
    {
        var assignment = await _repository
            .GetCurrentByEmploymentIdAsync(request.TenantId, request.EmploymentId, cancellationToken)
            .ConfigureAwait(false);

        return assignment is null
            ? Result.Failure<EmploymentAssignmentDto>(EmploymentErrors.EmploymentAssignmentNotFound)
            : Result.Success(EmploymentMapper.ToDto(assignment));
    }
}
