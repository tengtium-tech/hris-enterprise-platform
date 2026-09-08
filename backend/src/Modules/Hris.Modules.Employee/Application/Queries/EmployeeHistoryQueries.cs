using Hris.Application.Abstractions;
using Hris.Modules.Employee.Application.Dtos;
using Hris.Modules.Employee.Application.Mapping;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Queries;

/// <summary>
/// Returns immutable Employee-owned history (Lifecycle Stage transitions, and
/// personal/contact/government/banking information changes). Transfers,
/// promotions, and Employment Type / Operational Status changes are returned by
/// the Employment module's own history query, not this one -- see
/// queries.md's own "History Queries" section.
/// </summary>
public sealed record GetEmployeeHistoryQuery(Guid EmployeeId, Guid TenantId) : IQuery<Result<IReadOnlyList<EmployeeHistoryDto>>>;

internal sealed class GetEmployeeHistoryQueryHandler : IRequestHandler<GetEmployeeHistoryQuery, Result<IReadOnlyList<EmployeeHistoryDto>>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeHistoryRepository _historyRepository;

    public GetEmployeeHistoryQueryHandler(IEmployeeRepository employeeRepository, IEmployeeHistoryRepository historyRepository)
    {
        _employeeRepository = Guard.AgainstNull(employeeRepository, nameof(employeeRepository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
    }

    public async Task<Result<IReadOnlyList<EmployeeHistoryDto>>> Handle(GetEmployeeHistoryQuery request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _employeeRepository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<EmployeeHistoryDto>>(employeeResult.Error);
        }

        var history = await _historyRepository
            .ListByEmployeeIdAsync(request.TenantId, request.EmployeeId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<EmployeeHistoryDto> dtos = history.Select(EmployeeMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
