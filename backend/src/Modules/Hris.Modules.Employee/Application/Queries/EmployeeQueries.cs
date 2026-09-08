using Hris.Application.Abstractions;
using Hris.Modules.Employee.Application.Dtos;
using Hris.Modules.Employee.Application.Mapping;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Queries;

public sealed record GetEmployeeQuery(Guid EmployeeId, Guid TenantId) : IQuery<Result<EmployeeDto>>;

internal sealed class GetEmployeeQueryHandler : IRequestHandler<GetEmployeeQuery, Result<EmployeeDto>>
{
    private readonly IEmployeeRepository _repository;

    public GetEmployeeQueryHandler(IEmployeeRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<EmployeeDto>> Handle(GetEmployeeQuery request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employeeResult.IsFailure
            ? Result.Failure<EmployeeDto>(employeeResult.Error)
            : Result.Success(EmployeeMapper.ToDto(employeeResult.Value));
    }
}

public sealed record ListEmployeesQuery(Guid TenantId, string? LifecycleStageFilter) : IQuery<Result<IReadOnlyList<EmployeeSummaryDto>>>;

internal sealed class ListEmployeesQueryHandler : IRequestHandler<ListEmployeesQuery, Result<IReadOnlyList<EmployeeSummaryDto>>>
{
    private readonly IEmployeeRepository _repository;

    public ListEmployeesQueryHandler(IEmployeeRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<EmployeeSummaryDto>>> Handle(ListEmployeesQuery request, CancellationToken cancellationToken)
    {
        var employees = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IEnumerable<Domain.Employee> filtered = employees;
        if (!string.IsNullOrWhiteSpace(request.LifecycleStageFilter)
            && Enum.TryParse<EmployeeLifecycleStage>(request.LifecycleStageFilter, out var stage))
        {
            filtered = filtered.Where(employee => employee.LifecycleStage == stage);
        }

        IReadOnlyList<EmployeeSummaryDto> dtos = filtered.Select(EmployeeMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
