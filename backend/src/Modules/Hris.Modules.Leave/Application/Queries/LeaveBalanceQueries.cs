using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Application.Mapping;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Queries;

/// <summary>One employee's balance for one leave type. Source: application/queries.md.</summary>
public sealed record GetLeaveBalanceQuery(Guid TenantId, Guid EmployeeId, Guid LeaveTypeId) : IQuery<Result<LeaveBalanceDto>>;

internal sealed class GetLeaveBalanceQueryHandler : IRequestHandler<GetLeaveBalanceQuery, Result<LeaveBalanceDto>>
{
    private readonly ILeaveBalanceRepository _repository;

    public GetLeaveBalanceQueryHandler(ILeaveBalanceRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<LeaveBalanceDto>> Handle(GetLeaveBalanceQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balance = await _repository
            .GetByEmployeeAndLeaveTypeAsync(request.TenantId, request.EmployeeId, new LeaveTypeId(request.LeaveTypeId), cancellationToken)
            .ConfigureAwait(false);

        return balance is null
            ? Result.Failure<LeaveBalanceDto>(LeaveErrors.LeaveBalanceNotFound)
            : Result.Success(LeaveMapper.ToDto(balance));
    }
}

/// <summary>An employee's balances across every leave type. Source: application/queries.md.</summary>
public sealed record GetLeaveBalanceSummaryQuery(Guid TenantId, Guid EmployeeId) : IQuery<Result<IReadOnlyList<LeaveBalanceDto>>>;

internal sealed class GetLeaveBalanceSummaryQueryHandler
    : IRequestHandler<GetLeaveBalanceSummaryQuery, Result<IReadOnlyList<LeaveBalanceDto>>>
{
    private readonly ILeaveBalanceRepository _repository;

    public GetLeaveBalanceSummaryQueryHandler(ILeaveBalanceRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveBalanceDto>>> Handle(
        GetLeaveBalanceSummaryQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balances = await _repository.ListByEmployeeAsync(request.TenantId, request.EmployeeId, cancellationToken).ConfigureAwait(false);

        var summary = balances.Select(LeaveMapper.ToDto).ToList();
        return Result.Success((IReadOnlyList<LeaveBalanceDto>)summary);
    }
}

/// <summary>Full ledger entry history for a balance. Source: application/queries.md.</summary>
public sealed record GetLeaveLedgerQuery(Guid TenantId, Guid LeaveBalanceId) : IQuery<Result<IReadOnlyList<LeaveLedgerEntryDto>>>;

internal sealed class GetLeaveLedgerQueryHandler : IRequestHandler<GetLeaveLedgerQuery, Result<IReadOnlyList<LeaveLedgerEntryDto>>>
{
    private readonly ILeaveBalanceRepository _repository;

    public GetLeaveLedgerQueryHandler(ILeaveBalanceRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveLedgerEntryDto>>> Handle(GetLeaveLedgerQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balanceResult = await LeaveLookup
            .LoadLeaveBalanceForTenantAsync(_repository, request.LeaveBalanceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (balanceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<LeaveLedgerEntryDto>>(balanceResult.Error);
        }

        var ledger = balanceResult.Value.LedgerEntries
            .OrderBy(entry => entry.RecordedAt)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveLedgerEntryDto>)ledger);
    }
}
