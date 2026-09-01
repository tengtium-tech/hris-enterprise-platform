using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.RulesEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.RulesEngine.Application.Commands;

/// <summary>
/// Drafts the next version of an existing <see cref="RuleDefinition"/>, per
/// rules-engine.md's Rule Version section -- the identical shape
/// <c>CreateNewDraftVersionCommand</c> already establishes for Configuration
/// Framework's own sibling lifecycle. Authorization-gated the same way
/// <see cref="CreateRuleDefinitionCommand"/>'s own remarks explain.
/// </summary>
public sealed record CreateNewDraftVersionCommand(
    Guid RuleDefinitionId,
    IReadOnlyCollection<RuleConditionInput> Conditions,
    LogicalOperator ConditionOperator,
    IReadOnlyCollection<RuleActionInput> Actions,
    RulePriority Priority,
    Guid CreatedByUserId,
    OrganizationalScopeLevel ScopeLevel,
    Guid ScopeId) : ICommand<Result<Guid>>;

internal sealed class CreateNewDraftVersionCommandHandler : IRequestHandler<CreateNewDraftVersionCommand, Result<Guid>>
{
    private readonly IRuleDefinitionRepository _repository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public CreateNewDraftVersionCommandHandler(IRuleDefinitionRepository repository, ISender sender, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateNewDraftVersionCommand request, CancellationToken cancellationToken)
    {
        var authorization = await RuleAuthorizationCheck.CheckAsync(
            _sender, request.CreatedByUserId, request.ScopeLevel, request.ScopeId, cancellationToken).ConfigureAwait(false);
        if (authorization.IsFailure)
        {
            return Result.Failure<Guid>(authorization.Error);
        }

        var definition = await _repository
            .GetByIdAsync(new RuleDefinitionId(request.RuleDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return Result.Failure<Guid>(RuleErrors.RuleDefinitionNotFound);
        }

        var conditionsResult = CreateRuleDefinitionCommandHandler.BuildConditions(request.Conditions);
        if (conditionsResult.IsFailure)
        {
            return Result.Failure<Guid>(conditionsResult.Error);
        }

        var actionsResult = CreateRuleDefinitionCommandHandler.BuildActions(request.Actions);
        if (actionsResult.IsFailure)
        {
            return Result.Failure<Guid>(actionsResult.Error);
        }

        var versionResult = definition.CreateNewDraftVersion(
            conditionsResult.Value,
            request.ConditionOperator,
            actionsResult.Value,
            request.Priority,
            new UserAccountId(request.CreatedByUserId),
            _timeProvider.GetUtcNow());

        return versionResult.IsFailure
            ? Result.Failure<Guid>(versionResult.Error)
            : Result.Success(versionResult.Value.Id.Value);
    }
}
