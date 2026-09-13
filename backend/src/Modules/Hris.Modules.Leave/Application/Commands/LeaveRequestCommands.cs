using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Commands;

/// <summary>
/// Submits a leave request. Accepted even where eligibility will later fail (LV-030); that
/// failure surfaces as part of the approval decision. LV-031's overlap check spans every
/// non-terminal request this employee already has, so it is enforced here rather than
/// inside the aggregate. Source: application/commands.md (LeaveRequest Commands).
/// </summary>
public sealed record SubmitLeaveRequestCommand(
    Guid TenantId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    LeaveDateRange DateRange,
    StatutoryDetails? StatutoryDetails,
    bool RequiresStatutoryDetails,
    IReadOnlyList<string>? SupportingDocuments,
    Guid SubmittedBy) : ICommand<Result<Guid>>;

internal sealed class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Result<Guid>>
{
    private readonly ILeaveRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SubmitLeaveRequestCommandHandler(ILeaveRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var existing = await _repository
            .ListNonTerminalByEmployeeAsync(request.TenantId, request.EmployeeId, cancellationToken)
            .ConfigureAwait(false);
        if (existing.Any(other => other.DateRange.OverlapsWith(request.DateRange)))
        {
            return Result.Failure<Guid>(LeaveErrors.LeaveDateRangeOverlap);
        }

        var createResult = LeaveRequest.Create(
            new LeaveRequestId(Guid.NewGuid()), request.TenantId, request.EmployeeId, new LeaveTypeId(request.LeaveTypeId),
            request.DateRange, request.StatutoryDetails, request.RequiresStatutoryDetails, request.SupportingDocuments,
            request.SubmittedBy, _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>
/// Approves the request. <see cref="Domain.PayTreatment"/> is the caller's own resolution
/// of available balance and the effective policy at approval time (LV-037) — this handler
/// never re-derives it, and never writes to <c>LeaveBalance</c> itself (LV-040).
/// </summary>
public sealed record ApproveLeaveRequestCommand(
    Guid TenantId, Guid LeaveRequestId, Guid ApproverId, PayTreatment PayTreatment, decimal PaidDays, string? Comments)
    : ICommand<Result>;

internal sealed class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Result>
{
    private readonly ILeaveRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveLeaveRequestCommandHandler(ILeaveRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requestResult = await LeaveLookup
            .LoadLeaveRequestForTenantAsync(_repository, request.LeaveRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (requestResult.IsFailure)
        {
            return Result.Failure(requestResult.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var decision = new ApprovalDecision(request.ApproverId, ApprovalDecisionOutcome.Approved, nowUtc, request.Comments, null, null);

        return requestResult.Value.Approve(request.ApproverId, decision, request.PayTreatment, request.PaidDays, nowUtc);
    }
}

/// <summary>Rejects the request.</summary>
public sealed record RejectLeaveRequestCommand(Guid TenantId, Guid LeaveRequestId, Guid ApproverId, string Reason) : ICommand<Result>;

internal sealed class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, Result>
{
    private readonly ILeaveRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectLeaveRequestCommandHandler(ILeaveRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requestResult = await LeaveLookup
            .LoadLeaveRequestForTenantAsync(_repository, request.LeaveRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (requestResult.IsFailure)
        {
            return Result.Failure(requestResult.Error);
        }

        return requestResult.Value.Reject(request.ApproverId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>Cancels the request; raises <c>LeaveCancelled</c>, triggering a compensating entry if already deducted (LV-041).</summary>
public sealed record CancelLeaveRequestCommand(Guid TenantId, Guid LeaveRequestId, Guid ActorId, string Reason) : ICommand<Result>;

internal sealed class CancelLeaveRequestCommandHandler : IRequestHandler<CancelLeaveRequestCommand, Result>
{
    private readonly ILeaveRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelLeaveRequestCommandHandler(ILeaveRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requestResult = await LeaveLookup
            .LoadLeaveRequestForTenantAsync(_repository, request.LeaveRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (requestResult.IsFailure)
        {
            return Result.Failure(requestResult.Error);
        }

        return requestResult.Value.Cancel(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// System command issued by the <see cref="Domain.LeaveApproved"/> integration handler. Applies the
/// deduction to the targeted <c>LeaveBalance</c> in its own transaction (LV-040), creating it on first
/// touch if this is the employee's first-ever balance activity for the leave type (mirroring
/// <c>RecordLeaveAccrualCommand</c>'s own get-or-create shape). Never dispatched by a caller directly.
/// <paramref name="AllowNegativeBalance"/> is the caller's own resolution of the effective policy's
/// advance-leave provision (LV-022).
/// </summary>
public sealed record ApplyLeaveBalanceDeductionCommand(
    Guid TenantId, Guid EmployeeId, Guid LeaveTypeId, Guid LeaveRequestId, decimal Amount, DateOnly EffectiveDate,
    Guid ActorId, bool AllowNegativeBalance) : ICommand<Result>;

internal sealed class ApplyLeaveBalanceDeductionCommandHandler : IRequestHandler<ApplyLeaveBalanceDeductionCommand, Result>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApplyLeaveBalanceDeductionCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApplyLeaveBalanceDeductionCommand request, CancellationToken cancellationToken)
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
                return Result.Failure(createResult.Error);
            }

            balance = createResult.Value;
        }

        var deductionResult = balance.ApplyDeduction(
            request.LeaveRequestId, request.Amount, request.EffectiveDate, request.ActorId, request.AllowNegativeBalance,
            _timeProvider.GetUtcNow());
        if (deductionResult.IsFailure)
        {
            return Result.Failure(deductionResult.Error);
        }

        if (isNewBalance)
        {
            await _repository.AddAsync(balance, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}

/// <summary>
/// System command issued by the <see cref="Domain.LeaveCancelled"/> integration handler when the
/// cancelled request had already been deducted. The balance is expected to already exist — a
/// compensating entry can only follow a prior deduction that itself created one. Never dispatched by
/// a caller directly.
/// </summary>
public sealed record ApplyLeaveBalanceCompensationCommand(
    Guid TenantId, Guid EmployeeId, Guid LeaveTypeId, Guid LeaveRequestId, decimal Amount, DateOnly EffectiveDate,
    Guid ActorId) : ICommand<Result>;

internal sealed class ApplyLeaveBalanceCompensationCommandHandler : IRequestHandler<ApplyLeaveBalanceCompensationCommand, Result>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApplyLeaveBalanceCompensationCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApplyLeaveBalanceCompensationCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balance = await _repository
            .GetByEmployeeAndLeaveTypeAsync(request.TenantId, request.EmployeeId, new LeaveTypeId(request.LeaveTypeId), cancellationToken)
            .ConfigureAwait(false);
        if (balance is null)
        {
            return Result.Failure(LeaveErrors.LeaveBalanceNotFound);
        }

        return balance.ApplyCompensatingEntry(request.LeaveRequestId, request.Amount, request.EffectiveDate, request.ActorId, _timeProvider.GetUtcNow());
    }
}
