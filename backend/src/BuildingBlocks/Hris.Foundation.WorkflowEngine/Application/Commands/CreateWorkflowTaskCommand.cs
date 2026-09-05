using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Commands;

/// <summary>
/// Assigns a new <see cref="WorkflowTask"/> for a <see cref="WorkflowInstance"/>'s own
/// current Approval step -- called after <see cref="WorkflowInstance.RequestApproval"/>
/// succeeds, per that method's own remarks that this aggregate never creates the task
/// for itself.
/// </summary>
public sealed record CreateWorkflowTaskCommand(
    Guid TenantId,
    Guid WorkflowInstanceId,
    string StepName,
    int StepOrder,
    WorkflowParticipantType ParticipantType,
    string? ParticipantRoleName,
    Guid? AssignedToUserId) : ICommand<Result<Guid>>;

internal sealed class CreateWorkflowTaskCommandHandler : IRequestHandler<CreateWorkflowTaskCommand, Result<Guid>>
{
    private readonly IWorkflowTaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateWorkflowTaskCommandHandler(IWorkflowTaskRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateWorkflowTaskCommand request, CancellationToken cancellationToken)
    {
        var createResult = WorkflowTask.Create(
            request.TenantId, new WorkflowInstanceId(request.WorkflowInstanceId), request.StepName, request.StepOrder,
            request.ParticipantType, request.ParticipantRoleName, request.AssignedToUserId, escalationLevel: 0, _timeProvider.GetUtcNow());
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(createResult.Value.Id.Value);
    }
}
