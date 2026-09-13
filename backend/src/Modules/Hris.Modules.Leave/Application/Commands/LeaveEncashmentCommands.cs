using Hris.Application.Abstractions;
using Hris.Foundation.Entitlement.Application.Queries;
using Hris.Foundation.Entitlement.Domain;
using Hris.Modules.Leave.Application;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Commands;

/// <summary>
/// Submits a voluntary encashment request against a balance. Gated by Leave pack maturity
/// Level 3 (LV-074) — the highest maturity gate this module enforces, since encashment is
/// a discretionary cash-out capability rather than a correction (<c>LeaveAdjustment</c>,
/// Level 2) or an ordinary request (<c>LeaveRequest</c>, ungated). <see cref="IsCommutable"/>
/// and <see cref="MaximumCommutable"/> are the caller's own resolution of the effective
/// <c>LeavePolicy</c>'s <c>CommutabilityStatus</c> (LV-070) — mirroring how
/// <c>ApproveLeaveRequestCommand</c>'s own <c>PayTreatment</c> is caller-resolved rather than
/// re-derived here (Aggregate Design Rule 13). The targeted balance's current total, by
/// contrast, is loaded directly by this handler: it lives in this same module, so there is
/// no cross-module boundary to respect, and reading it fresh here (rather than trusting a
/// caller-supplied snapshot) is what LV-070's submission-time sufficiency check actually
/// requires. LV-072's duplicate-pending check spans multiple aggregate instances and so
/// belongs here rather than inside the aggregate. Source: application/commands.md
/// (LeaveEncashment Commands).
/// </summary>
public sealed record SubmitLeaveEncashmentCommand(
    Guid TenantId,
    TenantEditionCode Edition,
    Guid LeaveBalanceId,
    decimal RequestedAmount,
    bool IsCommutable,
    decimal? MaximumCommutable,
    Guid SubmittedBy) : ICommand<Result<Guid>>;

internal sealed class SubmitLeaveEncashmentCommandHandler : IRequestHandler<SubmitLeaveEncashmentCommand, Result<Guid>>
{
    private readonly ILeaveEncashmentRepository _repository;
    private readonly ILeaveBalanceRepository _balanceRepository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public SubmitLeaveEncashmentCommandHandler(
        ILeaveEncashmentRepository repository, ILeaveBalanceRepository balanceRepository, ISender sender, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _balanceRepository = Guard.AgainstNull(balanceRepository, nameof(balanceRepository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(SubmitLeaveEncashmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var entitlement = await _sender
            .Send(new EvaluateEntitlementQuery(request.Edition, ProcessPackCode.Leave, MaturityLevel.Advanced), cancellationToken)
            .ConfigureAwait(false);
        if (entitlement.IsFailure)
        {
            return Result.Failure<Guid>(entitlement.Error);
        }

        if (!entitlement.Value.IsEntitled)
        {
            return Result.Failure<Guid>(LeaveErrors.EncashmentNotEntitled);
        }

        var balanceResult = await LeaveLookup
            .LoadLeaveBalanceForTenantAsync(_balanceRepository, request.LeaveBalanceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (balanceResult.IsFailure)
        {
            return Result.Failure<Guid>(balanceResult.Error);
        }

        var leaveBalanceId = new LeaveBalanceId(request.LeaveBalanceId);
        var pending = await _repository
            .ListPendingByBalanceAsync(request.TenantId, leaveBalanceId, cancellationToken)
            .ConfigureAwait(false);
        if (pending.Count > 0)
        {
            return Result.Failure<Guid>(LeaveErrors.DuplicateEncashmentPending);
        }

        var createResult = LeaveEncashment.Create(
            new LeaveEncashmentId(Guid.NewGuid()), request.TenantId, balanceResult.Value.EmployeeId, leaveBalanceId,
            request.RequestedAmount, request.IsCommutable, request.MaximumCommutable, balanceResult.Value.CurrentBalance,
            request.SubmittedBy, _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>Approves the request (<c>HRManager</c>, LV-073). Modifies only this aggregate; a separate transaction applies the outcome to <c>LeaveBalance</c> (LV-071).</summary>
public sealed record ApproveLeaveEncashmentCommand(Guid TenantId, Guid LeaveEncashmentId, Guid ApproverId, string? Comments) : ICommand<Result>;

internal sealed class ApproveLeaveEncashmentCommandHandler : IRequestHandler<ApproveLeaveEncashmentCommand, Result>
{
    private readonly ILeaveEncashmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveLeaveEncashmentCommandHandler(ILeaveEncashmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveLeaveEncashmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var encashmentResult = await LeaveLookup
            .LoadLeaveEncashmentForTenantAsync(_repository, request.LeaveEncashmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (encashmentResult.IsFailure)
        {
            return Result.Failure(encashmentResult.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var decision = new ApprovalDecision(request.ApproverId, ApprovalDecisionOutcome.Approved, nowUtc, request.Comments, null, null);

        return encashmentResult.Value.Approve(request.ApproverId, decision, nowUtc);
    }
}

/// <summary>Rejects the request.</summary>
public sealed record RejectLeaveEncashmentCommand(Guid TenantId, Guid LeaveEncashmentId, Guid ApproverId, string Reason) : ICommand<Result>;

internal sealed class RejectLeaveEncashmentCommandHandler : IRequestHandler<RejectLeaveEncashmentCommand, Result>
{
    private readonly ILeaveEncashmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectLeaveEncashmentCommandHandler(ILeaveEncashmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectLeaveEncashmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var encashmentResult = await LeaveLookup
            .LoadLeaveEncashmentForTenantAsync(_repository, request.LeaveEncashmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (encashmentResult.IsFailure)
        {
            return Result.Failure(encashmentResult.Error);
        }

        return encashmentResult.Value.Reject(request.ApproverId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>Cancels the request; only permitted while pending (stricter than <c>LeaveRequest.Cancel</c>'s after-the-fact case — see <see cref="LeaveEncashment.Cancel"/>).</summary>
public sealed record CancelLeaveEncashmentCommand(Guid TenantId, Guid LeaveEncashmentId, Guid ActorId, string Reason) : ICommand<Result>;

internal sealed class CancelLeaveEncashmentCommandHandler : IRequestHandler<CancelLeaveEncashmentCommand, Result>
{
    private readonly ILeaveEncashmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelLeaveEncashmentCommandHandler(ILeaveEncashmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelLeaveEncashmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var encashmentResult = await LeaveLookup
            .LoadLeaveEncashmentForTenantAsync(_repository, request.LeaveEncashmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (encashmentResult.IsFailure)
        {
            return Result.Failure(encashmentResult.Error);
        }

        return encashmentResult.Value.Cancel(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// System command issued by the <see cref="Domain.LeaveEncashmentApproved"/> integration handler.
/// Applies the encashed amount to the targeted <c>LeaveBalance</c> in its own transaction (LV-071).
/// Never dispatched by a caller directly. Unlike <c>ApplyLeaveAdjustmentCommand</c>, no confirmation
/// event follows: LV-073 makes <see cref="LeaveEncashmentStatus.Approved"/> this module's own
/// terminal state for this Sprint, so there is nothing further to close out here.
/// </summary>
public sealed record ApplyLeaveEncashmentCommand(
    Guid TenantId, Guid LeaveBalanceId, Guid LeaveEncashmentId, decimal Amount, DateOnly EffectiveDate, Guid ActorId)
    : ICommand<Result>;

internal sealed class ApplyLeaveEncashmentCommandHandler : IRequestHandler<ApplyLeaveEncashmentCommand, Result>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApplyLeaveEncashmentCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApplyLeaveEncashmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balanceResult = await LeaveLookup
            .LoadLeaveBalanceForTenantAsync(_repository, request.LeaveBalanceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (balanceResult.IsFailure)
        {
            return Result.Failure(balanceResult.Error);
        }

        return balanceResult.Value.RecordEncashment(
            request.LeaveEncashmentId, request.Amount, request.EffectiveDate, request.ActorId, _timeProvider.GetUtcNow());
    }
}
