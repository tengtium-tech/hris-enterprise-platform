using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Commands;

/// <summary>
/// Records a scheduled accrual, creating the employee's balance for this leave type on
/// first accrual if none exists yet. Idempotent per employee per period (LV-024). No
/// actor — a scheduled process, never a person's decision (LV-092). Source:
/// application/commands.md (LeaveBalance Commands).
/// </summary>
public sealed record RecordLeaveAccrualCommand(
    Guid TenantId, Guid EmployeeId, Guid LeaveTypeId, Guid SourceReference, decimal Amount, DateOnly EffectiveDate)
    : ICommand<Result<Guid>>;

internal sealed class RecordLeaveAccrualCommandHandler : IRequestHandler<RecordLeaveAccrualCommand, Result<Guid>>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordLeaveAccrualCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RecordLeaveAccrualCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var leaveTypeId = new LeaveTypeId(request.LeaveTypeId);
        var balance = await _repository
            .GetByEmployeeAndLeaveTypeAsync(request.TenantId, request.EmployeeId, leaveTypeId, cancellationToken)
            .ConfigureAwait(false);

        var isNewBalance = balance is null;
        if (balance is null)
        {
            var createResult = LeaveBalance.Create(
                new LeaveBalanceId(Guid.NewGuid()), request.TenantId, request.EmployeeId, leaveTypeId);
            if (createResult.IsFailure)
            {
                return Result.Failure<Guid>(createResult.Error);
            }

            balance = createResult.Value;
        }

        var accrualResult = balance.RecordAccrual(request.SourceReference, request.Amount, request.EffectiveDate, _timeProvider.GetUtcNow());
        if (accrualResult.IsFailure)
        {
            return Result.Failure<Guid>(accrualResult.Error);
        }

        if (isNewBalance)
        {
            await _repository.AddAsync(balance, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(balance.Id.Value);
    }
}

/// <summary>
/// Records a Carryover entry at a period boundary. Issued by the (not yet built)
/// <c>LeaveCarryoverProcessor</c> domain service, one balance per transaction (LV-060).
/// </summary>
public sealed record RecordCarryoverCommand(
    Guid TenantId, Guid LeaveBalanceId, Guid SourceReference, decimal Amount, DateOnly EffectiveDate) : ICommand<Result>;

internal sealed class RecordCarryoverCommandHandler : IRequestHandler<RecordCarryoverCommand, Result>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordCarryoverCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecordCarryoverCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balanceResult = await LeaveLookup
            .LoadLeaveBalanceForTenantAsync(_repository, request.LeaveBalanceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (balanceResult.IsFailure)
        {
            return Result.Failure(balanceResult.Error);
        }

        return balanceResult.Value.RecordCarryover(request.SourceReference, request.Amount, request.EffectiveDate, _timeProvider.GetUtcNow());
    }
}

/// <summary>Records a Forfeiture entry for balance above the carryover cap, after any configured grace period (LV-061).</summary>
public sealed record RecordForfeitureCommand(
    Guid TenantId, Guid LeaveBalanceId, Guid SourceReference, decimal Amount, DateOnly EffectiveDate) : ICommand<Result>;

internal sealed class RecordForfeitureCommandHandler : IRequestHandler<RecordForfeitureCommand, Result>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordForfeitureCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecordForfeitureCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balanceResult = await LeaveLookup
            .LoadLeaveBalanceForTenantAsync(_repository, request.LeaveBalanceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (balanceResult.IsFailure)
        {
            return Result.Failure(balanceResult.Error);
        }

        return balanceResult.Value.RecordForfeiture(request.SourceReference, request.Amount, request.EffectiveDate, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Explicit verification/repair resummation (LV-025). Never a side effect of an ordinary
/// balance read; carries an actor, since — unlike accrual and carryover — this is a
/// person-initiated administrative action.
/// </summary>
public sealed record RecalculateLeaveBalanceCommand(Guid TenantId, Guid LeaveBalanceId, Guid ActorId) : ICommand<Result>;

internal sealed class RecalculateLeaveBalanceCommandHandler : IRequestHandler<RecalculateLeaveBalanceCommand, Result>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecalculateLeaveBalanceCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecalculateLeaveBalanceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balanceResult = await LeaveLookup
            .LoadLeaveBalanceForTenantAsync(_repository, request.LeaveBalanceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (balanceResult.IsFailure)
        {
            return Result.Failure(balanceResult.Error);
        }

        return balanceResult.Value.Recalculate(request.ActorId, _timeProvider.GetUtcNow());
    }
}
