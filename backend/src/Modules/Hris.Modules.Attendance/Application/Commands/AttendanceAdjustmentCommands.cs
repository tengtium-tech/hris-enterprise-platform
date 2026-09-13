using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Commands;

/// <summary>
/// Submits a correction request against a record. The original value is snapshotted at
/// submission, never read live at approval (AT-020). Creating the adjustment raises
/// <see cref="AttendanceAdjustmentSubmitted"/>, which the integration layer turns into a
/// <see cref="MarkRecordAdjustmentSubmittedCommand"/> on the record (AT-031). Source:
/// application/commands.md (AttendanceAdjustment Commands).
/// </summary>
public sealed record SubmitAttendanceAdjustmentCommand(
    Guid TenantId,
    Guid AttendanceRecordId,
    string Field,
    string OriginalValue,
    string RequestedValue,
    AdjustmentCategory Category,
    string Reason,
    IReadOnlyList<string>? SupportingDocuments,
    Guid SubmittedBy) : ICommand<Result<Guid>>;

internal sealed class SubmitAttendanceAdjustmentCommandHandler
    : IRequestHandler<SubmitAttendanceAdjustmentCommand, Result<Guid>>
{
    private readonly IAttendanceAdjustmentRepository _repository;
    private readonly IAttendanceRecordRepository _recordRepository;
    private readonly TimeProvider _timeProvider;

    public SubmitAttendanceAdjustmentCommandHandler(
        IAttendanceAdjustmentRepository repository, IAttendanceRecordRepository recordRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _recordRepository = Guard.AgainstNull(recordRepository, nameof(recordRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(SubmitAttendanceAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_recordRepository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure<Guid>(recordResult.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var createResult = AttendanceAdjustment.Create(
            new AttendanceAdjustmentId(Guid.NewGuid()),
            request.TenantId,
            new AttendanceRecordId(request.AttendanceRecordId),
            recordResult.Value.WorkDate,
            request.Field,
            request.OriginalValue,
            request.RequestedValue,
            request.Category,
            request.Reason,
            request.SupportingDocuments,
            request.SubmittedBy,
            nowUtc);

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>Records a reviewer's notes and moves the adjustment into review.</summary>
public sealed record ReviewAttendanceAdjustmentCommand(
    Guid TenantId, Guid AttendanceAdjustmentId, Guid ReviewerId, string Notes) : ICommand<Result>;

internal sealed class ReviewAttendanceAdjustmentCommandHandler
    : IRequestHandler<ReviewAttendanceAdjustmentCommand, Result>
{
    private readonly IAttendanceAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReviewAttendanceAdjustmentCommandHandler(IAttendanceAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReviewAttendanceAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await AttendanceLookup
            .LoadAdjustmentForTenantAsync(_repository, request.AttendanceAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.Review(request.ReviewerId, request.Notes, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Approves the adjustment. This handler modifies only <see cref="AttendanceAdjustment"/> and
/// raises <see cref="AttendanceAdjustmentApproved"/>; a separate transaction then applies the
/// outcome to the record (AT-024), so this handler never reaches into the record directly.
/// </summary>
public sealed record ApproveAttendanceAdjustmentCommand(
    Guid TenantId,
    Guid AttendanceAdjustmentId,
    Guid ApproverId,
    ApprovalDecisionOutcome Outcome,
    string? Comments,
    Guid? DelegateId,
    Guid? OriginalApproverId) : ICommand<Result>;

internal sealed class ApproveAttendanceAdjustmentCommandHandler
    : IRequestHandler<ApproveAttendanceAdjustmentCommand, Result>
{
    private readonly IAttendanceAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveAttendanceAdjustmentCommandHandler(IAttendanceAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveAttendanceAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await AttendanceLookup
            .LoadAdjustmentForTenantAsync(_repository, request.AttendanceAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var decision = new ApprovalDecision(
            request.ApproverId, request.Outcome, nowUtc, request.Comments, request.DelegateId, request.OriginalApproverId);

        return adjustmentResult.Value.Approve(request.ApproverId, decision, nowUtc);
    }
}

/// <summary>Rejects the adjustment; the record's pending-adjustment block is then released.</summary>
public sealed record RejectAttendanceAdjustmentCommand(
    Guid TenantId, Guid AttendanceAdjustmentId, Guid ApproverId, string Reason) : ICommand<Result>;

internal sealed class RejectAttendanceAdjustmentCommandHandler
    : IRequestHandler<RejectAttendanceAdjustmentCommand, Result>
{
    private readonly IAttendanceAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectAttendanceAdjustmentCommandHandler(IAttendanceAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectAttendanceAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await AttendanceLookup
            .LoadAdjustmentForTenantAsync(_repository, request.AttendanceAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.Reject(request.ApproverId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>Cancels the adjustment; the record's pending-adjustment block is then released.</summary>
public sealed record CancelAttendanceAdjustmentCommand(
    Guid TenantId, Guid AttendanceAdjustmentId, Guid ActorId, string Reason) : ICommand<Result>;

internal sealed class CancelAttendanceAdjustmentCommandHandler
    : IRequestHandler<CancelAttendanceAdjustmentCommand, Result>
{
    private readonly IAttendanceAdjustmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelAttendanceAdjustmentCommandHandler(IAttendanceAdjustmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelAttendanceAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await AttendanceLookup
            .LoadAdjustmentForTenantAsync(_repository, request.AttendanceAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.Cancel(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// System command issued by the <c>AttendanceAdjustmentApproved</c> integration handler. Applies
/// the approved outcome to the targeted <see cref="AttendanceRecord"/> in its own transaction,
/// decrementing the pending-adjustment count (AT-031). Never dispatched by a caller directly.
/// Source: application/commands.md (AttendanceAdjustment Commands).
/// </summary>
public sealed record ApplyAttendanceAdjustmentCommand(
    Guid TenantId, Guid AttendanceRecordId, Guid AttendanceAdjustmentId, string Field, string RequestedValue)
    : ICommand<Result>;

internal sealed class ApplyAttendanceAdjustmentCommandHandler
    : IRequestHandler<ApplyAttendanceAdjustmentCommand, Result>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApplyAttendanceAdjustmentCommandHandler(IAttendanceRecordRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApplyAttendanceAdjustmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var recordResult = await AttendanceLookup
            .LoadRecordForTenantAsync(_repository, request.AttendanceRecordId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (recordResult.IsFailure)
        {
            return Result.Failure(recordResult.Error);
        }

        return recordResult.Value.ApplyAdjustment(
            new AttendanceAdjustmentId(request.AttendanceAdjustmentId), request.Field, request.RequestedValue, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// System command issued by the <c>AttendanceAdjustmentApplied</c> integration handler to mark the
/// source adjustment Applied once the record incorporated its change (AT-024). Never dispatched by
/// a caller directly.
/// </summary>
public sealed record MarkAdjustmentAppliedCommand(Guid TenantId, Guid AttendanceAdjustmentId) : ICommand<Result>;

internal sealed class MarkAdjustmentAppliedCommandHandler : IRequestHandler<MarkAdjustmentAppliedCommand, Result>
{
    private readonly IAttendanceAdjustmentRepository _repository;

    public MarkAdjustmentAppliedCommandHandler(IAttendanceAdjustmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result> Handle(MarkAdjustmentAppliedCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var adjustmentResult = await AttendanceLookup
            .LoadAdjustmentForTenantAsync(_repository, request.AttendanceAdjustmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (adjustmentResult.IsFailure)
        {
            return Result.Failure(adjustmentResult.Error);
        }

        return adjustmentResult.Value.MarkApplied();
    }
}
