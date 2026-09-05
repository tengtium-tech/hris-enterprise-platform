using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Commands;

/// <summary>
/// Starts a new <see cref="WorkflowInstance"/> against a <see cref="WorkflowDefinition"/>'s
/// own currently published version -- the one cross-aggregate read
/// <see cref="WorkflowInstance.Trigger"/>'s own remarks say this aggregate cannot
/// perform for itself, so this handler performs it once, here, before calling that
/// factory.
/// </summary>
public sealed record TriggerWorkflowInstanceCommand(
    Guid TenantId,
    Guid WorkflowDefinitionId,
    string? TriggeringReference,
    Guid InitiatedByUserId) : ICommand<Result<Guid>>;

internal sealed class TriggerWorkflowInstanceCommandHandler : IRequestHandler<TriggerWorkflowInstanceCommand, Result<Guid>>
{
    private readonly IWorkflowDefinitionRepository _definitionRepository;
    private readonly IWorkflowInstanceRepository _instanceRepository;
    private readonly TimeProvider _timeProvider;

    public TriggerWorkflowInstanceCommandHandler(
        IWorkflowDefinitionRepository definitionRepository,
        IWorkflowInstanceRepository instanceRepository,
        TimeProvider timeProvider)
    {
        _definitionRepository = Guard.AgainstNull(definitionRepository, nameof(definitionRepository));
        _instanceRepository = Guard.AgainstNull(instanceRepository, nameof(instanceRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(TriggerWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        var definitionId = new WorkflowDefinitionId(request.WorkflowDefinitionId);

        var definition = await _definitionRepository.GetByIdAsync(definitionId, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure<Guid>(WorkflowEngineErrors.DefinitionNotFound);
        }

        var publishedVersion = definition.GetPublishedVersion();
        if (publishedVersion is null)
        {
            return Result.Failure<Guid>(WorkflowEngineErrors.VersionNotFound);
        }

        var triggerResult = WorkflowInstance.Trigger(
            request.TenantId, definitionId, publishedVersion.VersionNumber, request.TriggeringReference,
            request.InitiatedByUserId, _timeProvider.GetUtcNow());
        if (triggerResult.IsFailure)
        {
            return Result.Failure<Guid>(triggerResult.Error);
        }

        await _instanceRepository.AddAsync(triggerResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(triggerResult.Value.Id.Value);
    }
}
