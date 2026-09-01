using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.RulesEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.RulesEngine.Application.Commands;

/// <summary>
/// Creates a new <see cref="RuleDefinition"/> with its first Draft version, per
/// rules-engine.md's Rule Lifecycle ("Draft -&gt; Validated -&gt; ..."). One command per
/// coding-standards.md's Application Layer convention -- the identical shape
/// <c>CreateConfigurationSettingCommand</c> already establishes for the sibling
/// lifecycle this framework's own <see cref="RuleVersion"/> mirrors.
///
/// Carries raw primitives, not Domain Value Objects, across the MediatR boundary --
/// <see cref="CreateRuleDefinitionCommandHandler"/> is the one place a malformed key,
/// condition, or action becomes a <see cref="RuleErrors"/> failure.
///
/// Gated by an explicit authorization check, per rules-engine.md's own Security
/// Considerations: "Only authorized users should publish or modify business rules."
/// Unlike <c>EvaluateRuleQuery</c> (deliberately not gated -- see that query's own
/// remarks on why a per-evaluation check would fight this framework's own
/// millions-of-evaluations-daily NFR), rule management is an infrequent
/// administrative action, so the identical check
/// <c>SearchAuditRecordsQuery</c>/<c>GetAuditRecordByIdQuery</c> already perform costs
/// nothing this framework's own NFRs actually protect against. Reuses
/// <see cref="CreatedByUserId"/> as the checked principal rather than adding a
/// separate field -- the creator is the one whose authorization is in question here.
/// </summary>
public sealed record CreateRuleDefinitionCommand(
    string Key,
    string Category,
    IReadOnlyCollection<RuleConditionInput> Conditions,
    LogicalOperator ConditionOperator,
    IReadOnlyCollection<RuleActionInput> Actions,
    RulePriority Priority,
    Guid CreatedByUserId,
    OrganizationalScopeLevel ScopeLevel,
    Guid ScopeId) : ICommand<Result<Guid>>;

/// <summary>Raw shape of one <see cref="RuleCondition"/>, carried across the MediatR boundary.</summary>
public sealed record RuleConditionInput(string FieldName, ComparisonOperator Operator, string ComparisonValue);

/// <summary>Raw shape of one <see cref="RuleActionDirective"/>, carried across the MediatR boundary.</summary>
public sealed record RuleActionInput(string ActionKey, IReadOnlyDictionary<string, string>? Parameters = null);

internal sealed class CreateRuleDefinitionCommandHandler : IRequestHandler<CreateRuleDefinitionCommand, Result<Guid>>
{
    private readonly IRuleDefinitionRepository _repository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public CreateRuleDefinitionCommandHandler(IRuleDefinitionRepository repository, ISender sender, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateRuleDefinitionCommand request, CancellationToken cancellationToken)
    {
        var authorization = await RuleAuthorizationCheck.CheckAsync(
            _sender, request.CreatedByUserId, request.ScopeLevel, request.ScopeId, cancellationToken).ConfigureAwait(false);
        if (authorization.IsFailure)
        {
            return Result.Failure<Guid>(authorization.Error);
        }

        var keyResult = RuleKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<Guid>(keyResult.Error);
        }

        // rules-engine.md's own Rule Definition principle -- a rule is a named,
        // singular business policy -- means two definitions sharing a key would make
        // "the" rule for that key ambiguous, the same reasoning
        // CreateConfigurationSettingCommandHandler's own key+scope uniqueness check
        // documents for Configuration Framework.
        if (await _repository.GetByKeyAsync(keyResult.Value, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result.Failure<Guid>(RuleErrors.KeyAlreadyExists);
        }

        var conditionsResult = BuildConditions(request.Conditions);
        if (conditionsResult.IsFailure)
        {
            return Result.Failure<Guid>(conditionsResult.Error);
        }

        var actionsResult = BuildActions(request.Actions);
        if (actionsResult.IsFailure)
        {
            return Result.Failure<Guid>(actionsResult.Error);
        }

        var definitionResult = RuleDefinition.Create(
            keyResult.Value,
            request.Category,
            conditionsResult.Value,
            request.ConditionOperator,
            actionsResult.Value,
            request.Priority,
            new UserAccountId(request.CreatedByUserId),
            _timeProvider.GetUtcNow());

        if (definitionResult.IsFailure)
        {
            return Result.Failure<Guid>(definitionResult.Error);
        }

        await _repository.AddAsync(definitionResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(definitionResult.Value.Id.Value);
    }

    internal static Result<IReadOnlyCollection<RuleCondition>> BuildConditions(IReadOnlyCollection<RuleConditionInput> inputs)
    {
        var conditions = new List<RuleCondition>(inputs.Count);
        foreach (var input in inputs)
        {
            var result = RuleCondition.Create(input.FieldName, input.Operator, input.ComparisonValue);
            if (result.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<RuleCondition>>(result.Error);
            }

            conditions.Add(result.Value);
        }

        return Result.Success<IReadOnlyCollection<RuleCondition>>(conditions);
    }

    internal static Result<IReadOnlyCollection<RuleActionDirective>> BuildActions(IReadOnlyCollection<RuleActionInput> inputs)
    {
        var actions = new List<RuleActionDirective>(inputs.Count);
        foreach (var input in inputs)
        {
            var result = RuleActionDirective.Create(input.ActionKey, input.Parameters);
            if (result.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<RuleActionDirective>>(result.Error);
            }

            actions.Add(result.Value);
        }

        return Result.Success<IReadOnlyCollection<RuleActionDirective>>(actions);
    }
}
