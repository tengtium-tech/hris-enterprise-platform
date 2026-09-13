using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Commands;

/// <summary>Submits a pre-authorization request for overtime. Source: application/commands.md.</summary>
public sealed record SubmitOvertimeRequestCommand(
    Guid TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    TimeOnly? PlannedStart,
    TimeOnly? PlannedEnd,
    double EstimatedHours,
    OvertimeCategory Category,
    string Justification,
    Guid SubmittedBy) : ICommand<Result<Guid>>;

internal sealed class SubmitOvertimeRequestCommandHandler
    : IRequestHandler<SubmitOvertimeRequestCommand, Result<Guid>>
{
    private readonly IOvertimeRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SubmitOvertimeRequestCommandHandler(IOvertimeRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(SubmitOvertimeRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var createResult = OvertimeRequest.Create(
            new OvertimeRequestId(Guid.NewGuid()),
            request.TenantId,
            request.EmployeeId,
            request.WorkDate,
            request.PlannedStart,
            request.PlannedEnd,
            request.EstimatedHours,
            request.Category,
            request.Justification,
            request.SubmittedBy,
            _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>Approves the overtime request (AT-040: an approved request is payable).</summary>
public sealed record ApproveOvertimeRequestCommand(Guid TenantId, Guid OvertimeRequestId, Guid ApproverId) : ICommand<Result>;

internal sealed class ApproveOvertimeRequestCommandHandler : IRequestHandler<ApproveOvertimeRequestCommand, Result>
{
    private readonly IOvertimeRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveOvertimeRequestCommandHandler(IOvertimeRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveOvertimeRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requestResult = await AttendanceLookup
            .LoadOvertimeRequestForTenantAsync(_repository, request.OvertimeRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (requestResult.IsFailure)
        {
            return Result.Failure(requestResult.Error);
        }

        return requestResult.Value.Approve(request.ApproverId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Rejects the overtime request (AT-041: a rejected request is never payable).</summary>
public sealed record RejectOvertimeRequestCommand(Guid TenantId, Guid OvertimeRequestId, Guid ApproverId, string Reason)
    : ICommand<Result>;

internal sealed class RejectOvertimeRequestCommandHandler : IRequestHandler<RejectOvertimeRequestCommand, Result>
{
    private readonly IOvertimeRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectOvertimeRequestCommandHandler(IOvertimeRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectOvertimeRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requestResult = await AttendanceLookup
            .LoadOvertimeRequestForTenantAsync(_repository, request.OvertimeRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (requestResult.IsFailure)
        {
            return Result.Failure(requestResult.Error);
        }

        return requestResult.Value.Reject(request.ApproverId, request.Reason, _timeProvider.GetUtcNow());
    }
}

/// <summary>Cancels the overtime request.</summary>
public sealed record CancelOvertimeRequestCommand(Guid TenantId, Guid OvertimeRequestId, Guid ActorId, string Reason)
    : ICommand<Result>;

internal sealed class CancelOvertimeRequestCommandHandler : IRequestHandler<CancelOvertimeRequestCommand, Result>
{
    private readonly IOvertimeRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelOvertimeRequestCommandHandler(IOvertimeRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelOvertimeRequestCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var requestResult = await AttendanceLookup
            .LoadOvertimeRequestForTenantAsync(_repository, request.OvertimeRequestId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (requestResult.IsFailure)
        {
            return Result.Failure(requestResult.Error);
        }

        return requestResult.Value.Cancel(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}
