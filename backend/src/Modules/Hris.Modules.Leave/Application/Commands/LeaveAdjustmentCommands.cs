using Hris.Application.Abstractions;
using Hris.Foundation.Entitlement.Application.Queries;
using Hris.Foundation.Entitlement.Domain;
using Hris.Modules.Leave.Application;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Commands;

/// <summary>
/// Submits a correction request against a balance. Gated by Leave pack maturity Level 2
/// (LV-050), evaluated before authorization: a tenant below that level has no path to
/// submit an adjustment at all, not merely a restricted one — the platform's first command
/// handler to actually call <see cref="EvaluateEntitlementQuery"/> rather than stopping at
/// documentation, since every earlier module's own entitlement references never reached a
/// real command. <see cref="TenantEditionCode"/> is caller-supplied: no module in this
/// codebase yet resolves a tenant's edition from its identifier alone, so this follows the
/// same pre-resolved-primitive shape Attendance's own cross-module facts use. Also enforces
/// LV-053 (no duplicate pending adjustment against the same balance), which spans multiple
/// aggregate instances and so belongs here rather than inside the aggregate. Source:
/// application/commands.md (LeaveAdjustment Commands).
/// </summary>
public sealed record SubmitLeaveAdjustmentCommand(
    Guid TenantId,
    TenantEditionCode Edition,
    Guid LeaveBalanceId,
    decimal OriginalValueSnapshot,
    decimal RequestedAmount,
    string Reason,
    IReadOnlyList<string>? SupportingDocuments,
    Guid SubmittedBy) : ICommand<Result<Guid>>;

internal sealed class SubmitLeaveAdjustmentCommandHandler : IRequestHandler<SubmitLeaveAdjustmentCommand, Result<Guid>>
{
    private readonly ILeaveAdjustmentRepository _repository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public SubmitLeaveAdjustmentCommandHandler(ILeaveAdjustmentRepository repository, ISender sender, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(SubmitLeaveAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var entitlement = await _sender
            .Send(new EvaluateEntitlementQuery(request.Edition, ProcessPackCode.Leave, MaturityLevel.Standard), cancellationToken)
            .ConfigureAwait(false);
        if (entitlement.IsFailure)
        {
            return Result.Failure<Guid>(entitlement.Error);
        }

        if (!entitlement.Value.IsEntitled)
        {
            return Result.Failure<Guid>(LeaveErrors.AdjustmentNotEntitled);
        }

        var leaveBalanceId = new LeaveBalanceId(request.LeaveBalanceId);
        var pending = await _repository
            .ListPendingByBalanceAsync(request.TenantId, leaveBalanceId, cancellationToken)
            .ConfigureAwait(false);
        if (pending.Count > 0)
        {
            return Result.Failure<Guid>(LeaveErrors.DuplicateAdjustmentPending);
        }

        var createResult = LeaveAdjustment.Create(
            new LeaveAdjustmentId(Guid.NewGuid()), request.TenantId, leaveBalanceId, request.OriginalValueSnapshot,
            request.RequestedAmount, request.Reason, request.SupportingDocuments, request.SubmittedBy, _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>Records a reviewer's notes and moves the adjustment into review.</summary>
public sealed record ReviewLeaveAdjustmentCommand(Guid TenantId, Guid LeaveAdjustmentId, Guid ReviewerId, string Notes) : ICommand<Result>;

internal sealed class ReviewLeaveAdjustmentCommandHandler : IRequestHandler<ReviewLeaveAdjustmentCommand, Result>
{
    private readonly ILeaveAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReviewLeaveAdjustmentCommandHandler(ILeaveAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReviewLeaveAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await LeaveLookup
            .LoadLeaveAdjustmentForTenantAsync(_repository, request.LeaveAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.Review(request.ReviewerId, request.Notes, _timeProvider.GetUtcNow());
    }
}

/// <summary>Approves the adjustment (<c>HRManager</c>, LV-051). Modifies only this aggregate; a separate transaction applies the outcome to <c>LeaveBalance</c> (LV-052).</summary>
public sealed record ApproveLeaveAdjustmentCommand(
    Guid TenantId, Guid LeaveAdjustmentId, Guid ApproverId, string? Comments) : ICommand<Result>;

internal sealed class ApproveLeaveAdjustmentCommandHandler : IRequestHandler<ApproveLeaveAdjustmentCommand, Result>
{
    private readonly ILeaveAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveLeaveAdjustmentCommandHandler(ILeaveAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveLeaveAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await LeaveLookup
            .LoadLeaveAdjustmentForTenantAsync(_repository, request.LeaveAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var decision = new ApprovalDecision(request.ApproverId, ApprovalDecisionOutcome.Approved, nowUtc, request.Comments, null, null);

        return adjustmentResult.Value.Approve(request.ApproverId, decision, nowUtc);
    }
}

/// <summary>Rejects the adjustment.</summary>
public sealed record RejectLeaveAdjustmentCommand(Guid TenantId, Guid LeaveAdjustmentId, Guid ApproverId, string Reason) : ICommand<Result>;

internal sealed class RejectLeaveAdjustmentCommandHandler : IRequestHandler<RejectLeaveAdjustmentCommand, Result>
{
    private readonly ILeaveAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectLeaveAdjustmentCommandHandler(ILeaveAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectLeaveAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await LeaveLookup
            .LoadLeaveAdjustmentForTenantAsync(_repository, request.LeaveAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.Reject(request.ApproverId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>Cancels the adjustment.</summary>
public sealed record CancelLeaveAdjustmentCommand(Guid TenantId, Guid LeaveAdjustmentId, Guid ActorId, string Reason) : ICommand<Result>;

internal sealed class CancelLeaveAdjustmentCommandHandler : IRequestHandler<CancelLeaveAdjustmentCommand, Result>
{
    private readonly ILeaveAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelLeaveAdjustmentCommandHandler(ILeaveAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelLeaveAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await LeaveLookup
            .LoadLeaveAdjustmentForTenantAsync(_repository, request.LeaveAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.Cancel(_timeProvider.GetUtcNow());
    }
}

/// <summary>
/// System command issued by the <see cref="Domain.LeaveAdjustmentApproved"/> integration handler.
/// Applies the approved amount to the targeted <c>LeaveBalance</c> in its own transaction (LV-052).
/// Never dispatched by a caller directly.
/// </summary>
public sealed record ApplyLeaveAdjustmentCommand(
    Guid TenantId, Guid LeaveBalanceId, Guid LeaveAdjustmentId, decimal Amount, DateOnly EffectiveDate, Guid ActorId)
    : ICommand<Result>;

internal sealed class ApplyLeaveAdjustmentCommandHandler : IRequestHandler<ApplyLeaveAdjustmentCommand, Result>
{
    private readonly ILeaveBalanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApplyLeaveAdjustmentCommandHandler(ILeaveBalanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApplyLeaveAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var balanceResult = await LeaveLookup
            .LoadLeaveBalanceForTenantAsync(_repository, request.LeaveBalanceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (balanceResult.IsFailure)
        {
            return Result.Failure(balanceResult.Error);
        }

        return balanceResult.Value.RecordAdjustment(
            request.LeaveAdjustmentId, request.Amount, request.EffectiveDate, request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// System command issued by the <see cref="Domain.LeaveAdjustmentApplied"/> integration handler to
/// mark the source adjustment Applied once the balance incorporated its change (LV-052). Never
/// dispatched by a caller directly.
/// </summary>
public sealed record MarkLeaveAdjustmentAppliedCommand(Guid TenantId, Guid LeaveAdjustmentId) : ICommand<Result>;

internal sealed class MarkLeaveAdjustmentAppliedCommandHandler : IRequestHandler<MarkLeaveAdjustmentAppliedCommand, Result>
{
    private readonly ILeaveAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MarkLeaveAdjustmentAppliedCommandHandler(ILeaveAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MarkLeaveAdjustmentAppliedCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await LeaveLookup
            .LoadLeaveAdjustmentForTenantAsync(_repository, request.LeaveAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.MarkApplied(_timeProvider.GetUtcNow());
    }
}
