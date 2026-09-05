using Hris.Application.Abstractions;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.WorkflowEngine.Application.Commands;

/// <summary>
/// Registers a new workflow definition with its own first Draft version. Carries
/// <see cref="WorkflowStepDefinition"/> directly rather than raw per-field tuples --
/// unlike a Value Object requiring a <c>Create</c> parse step, this record is already
/// plain primitives (matching <c>SearchFieldDefinition</c>'s own "plain record, no
/// parsing needed" shape), so there is nothing for this handler to validate ahead of
/// the Domain layer's own <see cref="WorkflowDefinition.Create"/>.
/// </summary>
public sealed record CreateWorkflowDefinitionCommand(
    Guid TenantId,
    string Name,
    WorkflowTriggerType TriggerType,
    string? TriggerExpression,
    IReadOnlyList<WorkflowStepDefinition> Steps) : ICommand<Result<Guid>>;

internal sealed class CreateWorkflowDefinitionCommandHandler : IRequestHandler<CreateWorkflowDefinitionCommand, Result<Guid>>
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateWorkflowDefinitionCommandHandler(IWorkflowDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var createResult = WorkflowDefinition.Create(
            request.TenantId, request.Name, request.TriggerType, request.TriggerExpression, request.Steps, _timeProvider.GetUtcNow());
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(createResult.Value.Id.Value);
    }
}
