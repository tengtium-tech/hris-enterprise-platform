using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Commands;

public sealed record CreateNewWorkflowDefinitionDraftVersionCommand(
    Guid WorkflowDefinitionId,
    IReadOnlyList<WorkflowStepDefinition> Steps) : ICommand<Result<int>>;

public sealed record PublishWorkflowDefinitionVersionCommand(
    Guid WorkflowDefinitionId,
    int VersionNumber) : ICommand<Result>;

public sealed record DeprecateWorkflowDefinitionVersionCommand(
    Guid WorkflowDefinitionId,
    int VersionNumber) : ICommand<Result>;

internal sealed class CreateNewWorkflowDefinitionDraftVersionCommandHandler
    : IRequestHandler<CreateNewWorkflowDefinitionDraftVersionCommand, Result<int>>
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateNewWorkflowDefinitionDraftVersionCommandHandler(IWorkflowDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<int>> Handle(CreateNewWorkflowDefinitionDraftVersionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _repository.GetByIdAsync(
            new WorkflowDefinitionId(request.WorkflowDefinitionId), cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure<int>(WorkflowEngineErrors.DefinitionNotFound);
        }

        var result = definition.CreateNewDraftVersion(request.Steps, _timeProvider.GetUtcNow());
        return result.IsFailure
            ? Result.Failure<int>(result.Error)
            : Result.Success(result.Value.VersionNumber);
    }
}

/// <summary>
/// No <c>UpdateAsync</c> call in either handler below: a definition loaded through
/// <see cref="IWorkflowDefinitionRepository.GetByIdAsync"/> is already tracked by the
/// same <c>HrisDbContext</c>, so the caller's own <c>TransactionBehavior</c> persists
/// the mutation via change tracking alone -- the identical pattern every other Sprint
/// 3/4/5 framework's own lifecycle command handler already establishes.
/// </summary>
internal sealed class PublishWorkflowDefinitionVersionCommandHandler : IRequestHandler<PublishWorkflowDefinitionVersionCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishWorkflowDefinitionVersionCommandHandler(IWorkflowDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishWorkflowDefinitionVersionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _repository.GetByIdAsync(
            new WorkflowDefinitionId(request.WorkflowDefinitionId), cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure(WorkflowEngineErrors.DefinitionNotFound);
        }

        return definition.PublishVersion(request.VersionNumber, _timeProvider.GetUtcNow(), WorkflowCanonicalParticipantRoles.Names);
    }
}

internal sealed class DeprecateWorkflowDefinitionVersionCommandHandler : IRequestHandler<DeprecateWorkflowDefinitionVersionCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public DeprecateWorkflowDefinitionVersionCommandHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(DeprecateWorkflowDefinitionVersionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _repository.GetByIdAsync(
            new WorkflowDefinitionId(request.WorkflowDefinitionId), cancellationToken).ConfigureAwait(false);

        return definition is null
            ? Result.Failure(WorkflowEngineErrors.DefinitionNotFound)
            : definition.DeprecateVersion(request.VersionNumber);
    }
}
