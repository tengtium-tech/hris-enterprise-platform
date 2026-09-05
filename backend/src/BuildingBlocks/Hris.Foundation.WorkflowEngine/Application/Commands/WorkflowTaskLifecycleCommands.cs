using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Commands;

/// <summary>
/// Every <see cref="WorkflowTask"/> lifecycle transition bundled into one file, the
/// identical shape <c>WorkflowInstanceLifecycleCommands.cs</c> already establishes for
/// its own sibling aggregate.
/// </summary>
public sealed record ApproveWorkflowTaskCommand(Guid WorkflowTaskId, string? Comments) : ICommand<Result>;

public sealed record RejectWorkflowTaskCommand(Guid WorkflowTaskId, string? Comments) : ICommand<Result>;

public sealed record DelegateWorkflowTaskCommand(Guid WorkflowTaskId, Guid DelegateToUserId, string? Reason) : ICommand<Result>;

/// <summary>
/// Closes the current task as escalated and creates a new <see cref="WorkflowTask"/>
/// for the escalation target in the same operation, per
/// <see cref="WorkflowTask.Escalate"/>'s own remarks -- both writes are tracked by the
/// same <c>HrisDbContext</c> and persist atomically through the caller's own
/// <c>TransactionBehavior</c>, so neither the closed task nor the new one is ever
/// observed without the other.
/// </summary>
public sealed record EscalateWorkflowTaskCommand(Guid WorkflowTaskId, Guid EscalateToUserId) : ICommand<Result<Guid>>;

public sealed record ExpireWorkflowTaskCommand(Guid WorkflowTaskId) : ICommand<Result>;

public sealed record CancelWorkflowTaskCommand(Guid WorkflowTaskId) : ICommand<Result>;

internal sealed class ApproveWorkflowTaskCommandHandler : IRequestHandler<ApproveWorkflowTaskCommand, Result>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveWorkflowTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new WorkflowTaskId(request.WorkflowTaskId), cancellationToken).ConfigureAwait(false);
        return task is null
            ? Result.Failure(WorkflowEngineErrors.TaskNotFound)
            : task.Approve(request.Comments, _timeProvider.GetUtcNow());
    }
}

internal sealed class RejectWorkflowTaskCommandHandler : IRequestHandler<RejectWorkflowTaskCommand, Result>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectWorkflowTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new WorkflowTaskId(request.WorkflowTaskId), cancellationToken).ConfigureAwait(false);
        return task is null
            ? Result.Failure(WorkflowEngineErrors.TaskNotFound)
            : task.Reject(request.Comments, _timeProvider.GetUtcNow());
    }
}

internal sealed class DelegateWorkflowTaskCommandHandler : IRequestHandler<DelegateWorkflowTaskCommand, Result>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DelegateWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DelegateWorkflowTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new WorkflowTaskId(request.WorkflowTaskId), cancellationToken).ConfigureAwait(false);
        return task is null
            ? Result.Failure(WorkflowEngineErrors.TaskNotFound)
            : task.Delegate(request.DelegateToUserId, request.Reason, _timeProvider.GetUtcNow());
    }
}

internal sealed class EscalateWorkflowTaskCommandHandler : IRequestHandler<EscalateWorkflowTaskCommand, Result<Guid>>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EscalateWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(EscalateWorkflowTaskCommand request, CancellationToken cancellationToken)
    {
        if (request.EscalateToUserId == Guid.Empty)
        {
            return Result.Failure<Guid>(WorkflowEngineErrors.EscalateToUserRequired);
        }

        var task = await _repository.GetByIdAsync(new WorkflowTaskId(request.WorkflowTaskId), cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            return Result.Failure<Guid>(WorkflowEngineErrors.TaskNotFound);
        }

        var nowUtc = _timeProvider.GetUtcNow();

        var escalateResult = task.Escalate(nowUtc);
        if (escalateResult.IsFailure)
        {
            return Result.Failure<Guid>(escalateResult.Error);
        }

        var newTaskResult = WorkflowTask.Create(
            task.TenantId, task.WorkflowInstanceId, task.StepName, task.StepOrder, task.ParticipantType,
            task.ParticipantRoleName, request.EscalateToUserId, task.EscalationLevel + 1, nowUtc);
        if (newTaskResult.IsFailure)
        {
            return Result.Failure<Guid>(newTaskResult.Error);
        }

        await _repository.AddAsync(newTaskResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(newTaskResult.Value.Id.Value);
    }
}

internal sealed class ExpireWorkflowTaskCommandHandler : IRequestHandler<ExpireWorkflowTaskCommand, Result>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpireWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ExpireWorkflowTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new WorkflowTaskId(request.WorkflowTaskId), cancellationToken).ConfigureAwait(false);
        return task is null ? Result.Failure(WorkflowEngineErrors.TaskNotFound) : task.Expire(_timeProvider.GetUtcNow());
    }
}

internal sealed class CancelWorkflowTaskCommandHandler : IRequestHandler<CancelWorkflowTaskCommand, Result>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelWorkflowTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new WorkflowTaskId(request.WorkflowTaskId), cancellationToken).ConfigureAwait(false);
        return task is null ? Result.Failure(WorkflowEngineErrors.TaskNotFound) : task.Cancel(_timeProvider.GetUtcNow());
    }
}
