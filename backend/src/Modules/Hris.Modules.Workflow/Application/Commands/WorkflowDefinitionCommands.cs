using Hris.Application.Abstractions;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Workflow.Application.Commands;

/// <summary>
/// Authors a new definition in Draft. Source:
/// docs/04-modules/workflow/application/commands.md's own Definition Commands table.
/// </summary>
public sealed record AuthorWorkflowDefinitionCommand(
    Guid TenantId,
    Guid BusinessProcessId,
    string? Name,
    string? Description,
    string? TriggerCondition,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class AuthorWorkflowDefinitionCommandHandler : IRequestHandler<AuthorWorkflowDefinitionCommand, Result<Guid>>
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AuthorWorkflowDefinitionCommandHandler(IWorkflowDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AuthorWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var nameCollides = !string.IsNullOrWhiteSpace(request.Name)
            && await _repository.NameExistsForBusinessProcessAsync(
                request.TenantId, request.BusinessProcessId, request.Name.Trim(), null, cancellationToken).ConfigureAwait(false);

        var id = new WorkflowDefinitionId(Guid.NewGuid());
        var result = WorkflowDefinition.Author(
            id, request.TenantId, request.BusinessProcessId, request.Name, request.Description, request.TriggerCondition,
            nameCollides, request.ActingUser, _timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(result.Value.Id.Value);
    }
}

/// <summary>
/// Replaces a draft's step set. Only ever against a Draft: there is deliberately no
/// EditPublishedDefinitionCommand, because WR-004 makes a published definition
/// immutable and a change is a new version regardless of how small it is.
/// </summary>
public sealed record EditDraftDefinitionCommand(
    Guid TenantId,
    Guid DefinitionId,
    IReadOnlyList<WorkflowStepInput> Steps,
    Guid ActingUser) : ICommand<Result>;

internal sealed class EditDraftDefinitionCommandHandler : IRequestHandler<EditDraftDefinitionCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public EditDraftDefinitionCommandHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(EditDraftDefinitionCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var definitionResult = await WorkflowLookup
            .LoadDefinitionForTenantAsync(_repository, request.DefinitionId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (definitionResult.IsFailure)
        {
            return Result.Failure(definitionResult.Error);
        }

        var steps = new List<WorkflowStep>();
        foreach (var input in request.Steps)
        {
            var stepResult = input.ToStep();
            if (stepResult.IsFailure)
            {
                return Result.Failure(stepResult.Error);
            }

            steps.Add(stepResult.Value);
        }

        return definitionResult.Value.ReplaceSteps(steps);
    }
}

/// <summary>
/// Runs whole-graph validation and makes the definition executable. This command
/// rejects far more than it accepts, and every rejection carries the specific
/// invariant that failed rather than a generic failure the author would have to
/// re-derive.
///
/// The two checks the aggregate cannot make for itself are computed here.
/// <c>anyReferencedCommandMissing</c> would consult a registry of every module's
/// public command surface, which no module in this codebase publishes yet, so it is
/// carried on the command as a caller-supplied signal and defaults to permissive;
/// the gap is tracked in STATUS.md rather than silently invented.
/// <c>anyStepCanRouteToRequester</c> is the publication half of CTR-WFL-002 and is
/// likewise caller-supplied, since deciding it needs role-holder data this module
/// does not own. Note that the structural half of that requirement is already
/// unconditional and needs no caller cooperation at all, because
/// <see cref="ApproverResolutionRule"/> cannot express a named individual.
/// </summary>
public sealed record PublishWorkflowDefinitionCommand(
    Guid TenantId,
    Guid DefinitionId,
    bool AnyReferencedCommandMissing,
    bool AnyStepCanRouteToRequester,
    bool TriggerConditionOverlapsAnotherDefinition,
    Guid ActingUser) : ICommand<Result>;

internal sealed class PublishWorkflowDefinitionCommandHandler : IRequestHandler<PublishWorkflowDefinitionCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishWorkflowDefinitionCommandHandler(IWorkflowDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var definitionResult = await WorkflowLookup
            .LoadDefinitionForTenantAsync(_repository, request.DefinitionId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return definitionResult.IsFailure
            ? Result.Failure(definitionResult.Error)
            : definitionResult.Value.Publish(
                request.AnyReferencedCommandMissing, request.AnyStepCanRouteToRequester,
                request.TriggerConditionOverlapsAnotherDefinition, request.ActingUser, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Copies the current published version into a new Draft sharing its lineage. The
/// new version is a separate aggregate instance, never an edit to the old one, so
/// an instance already bound to the previous version can rely on it never changing
/// underneath (WR-050).
/// </summary>
public sealed record CreateNewVersionCommand(Guid TenantId, Guid DefinitionId, Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class CreateNewVersionCommandHandler : IRequestHandler<CreateNewVersionCommand, Result<Guid>>
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateNewVersionCommandHandler(IWorkflowDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateNewVersionCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var definitionResult = await WorkflowLookup
            .LoadDefinitionForTenantAsync(_repository, request.DefinitionId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (definitionResult.IsFailure)
        {
            return Result.Failure<Guid>(definitionResult.Error);
        }

        var newId = new WorkflowDefinitionId(Guid.NewGuid());
        var versionResult = definitionResult.Value.CreateNewVersion(newId, request.ActingUser, _timeProvider.GetUtcNow());
        if (versionResult.IsFailure)
        {
            return Result.Failure<Guid>(versionResult.Error);
        }

        await _repository.AddAsync(versionResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(versionResult.Value.Id.Value);
    }
}

/// <summary>
/// Withdraws a definition from new instances. Idempotent by convergence, per
/// commands.md. Running instances are unaffected, because deprecation is
/// forward-looking only and an instance binds to its version once at creation.
/// </summary>
public sealed record DeprecateWorkflowDefinitionCommand(
    Guid TenantId, Guid DefinitionId, Guid ActingUser, string? Reason) : ICommand<Result>;

internal sealed class DeprecateWorkflowDefinitionCommandHandler : IRequestHandler<DeprecateWorkflowDefinitionCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeprecateWorkflowDefinitionCommandHandler(IWorkflowDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeprecateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var definitionResult = await WorkflowLookup
            .LoadDefinitionForTenantAsync(_repository, request.DefinitionId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return definitionResult.IsFailure
            ? Result.Failure(definitionResult.Error)
            : definitionResult.Value.Deprecate(request.ActingUser, request.Reason, _timeProvider.GetUtcNow());
    }
}
