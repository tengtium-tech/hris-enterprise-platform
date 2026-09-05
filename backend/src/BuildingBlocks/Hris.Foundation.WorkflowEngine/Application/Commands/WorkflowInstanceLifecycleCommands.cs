using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Commands;

/// <summary>
/// Every <see cref="WorkflowInstance"/> lifecycle transition bundled into one file, the
/// identical shape <c>JobLifecycleCommands.cs</c>/<c>ScheduleLifecycleCommands.cs</c>
/// already establish for their own aggregate's own transitions.
/// </summary>
public sealed record AdvanceWorkflowInstanceCommand(Guid WorkflowInstanceId, int NextStepOrder) : ICommand<Result>;

public sealed record RequestWorkflowInstanceApprovalCommand(Guid WorkflowInstanceId) : ICommand<Result>;

public sealed record ResumeWorkflowInstanceAfterApprovalCommand(Guid WorkflowInstanceId, int NextStepOrder) : ICommand<Result>;

public sealed record RejectWorkflowInstanceCommand(Guid WorkflowInstanceId, string Reason) : ICommand<Result>;

public sealed record CancelWorkflowInstanceCommand(Guid WorkflowInstanceId, string Reason) : ICommand<Result>;

public sealed record WithdrawWorkflowInstanceCommand(Guid WorkflowInstanceId) : ICommand<Result>;

public sealed record ExpireWorkflowInstanceCommand(Guid WorkflowInstanceId) : ICommand<Result>;

public sealed record FailWorkflowInstanceCommand(Guid WorkflowInstanceId, string Reason) : ICommand<Result>;

public sealed record CompleteWorkflowInstanceCommand(Guid WorkflowInstanceId) : ICommand<Result>;

internal sealed class AdvanceWorkflowInstanceCommandHandler : IRequestHandler<AdvanceWorkflowInstanceCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AdvanceWorkflowInstanceCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(AdvanceWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.Advance(request.NextStepOrder, _timeProvider.GetUtcNow());
    }
}

internal sealed class RequestWorkflowInstanceApprovalCommandHandler : IRequestHandler<RequestWorkflowInstanceApprovalCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;

    public RequestWorkflowInstanceApprovalCommandHandler(IWorkflowInstanceRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(RequestWorkflowInstanceApprovalCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null ? Result.Failure(WorkflowEngineErrors.InstanceNotFound) : instance.RequestApproval();
    }
}

internal sealed class ResumeWorkflowInstanceAfterApprovalCommandHandler
    : IRequestHandler<ResumeWorkflowInstanceAfterApprovalCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ResumeWorkflowInstanceAfterApprovalCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ResumeWorkflowInstanceAfterApprovalCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.ResumeAfterApproval(request.NextStepOrder, _timeProvider.GetUtcNow());
    }
}

internal sealed class RejectWorkflowInstanceCommandHandler : IRequestHandler<RejectWorkflowInstanceCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RejectWorkflowInstanceCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RejectWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.Reject(request.Reason, _timeProvider.GetUtcNow());
    }
}

internal sealed class CancelWorkflowInstanceCommandHandler : IRequestHandler<CancelWorkflowInstanceCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelWorkflowInstanceCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.Cancel(request.Reason, _timeProvider.GetUtcNow());
    }
}

internal sealed class WithdrawWorkflowInstanceCommandHandler : IRequestHandler<WithdrawWorkflowInstanceCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public WithdrawWorkflowInstanceCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(WithdrawWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.Withdraw(_timeProvider.GetUtcNow());
    }
}

internal sealed class ExpireWorkflowInstanceCommandHandler : IRequestHandler<ExpireWorkflowInstanceCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpireWorkflowInstanceCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ExpireWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.Expire(_timeProvider.GetUtcNow());
    }
}

internal sealed class FailWorkflowInstanceCommandHandler : IRequestHandler<FailWorkflowInstanceCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FailWorkflowInstanceCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(FailWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.Fail(request.Reason, _timeProvider.GetUtcNow());
    }
}

internal sealed class CompleteWorkflowInstanceCommandHandler : IRequestHandler<CompleteWorkflowInstanceCommand, Result>
{
    private readonly IWorkflowInstanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteWorkflowInstanceCommandHandler(IWorkflowInstanceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CompleteWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            new WorkflowInstanceId(request.WorkflowInstanceId), cancellationToken).ConfigureAwait(false);

        return instance is null
            ? Result.Failure(WorkflowEngineErrors.InstanceNotFound)
            : instance.Complete(_timeProvider.GetUtcNow());
    }
}
