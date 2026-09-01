using Hris.Foundation.RulesEngine.Application.Dtos;
using Hris.Foundation.RulesEngine.Domain;

namespace Hris.Foundation.RulesEngine.Application.Mapping;

/// <summary>
/// Maps <see cref="RuleDefinition"/>/<see cref="RuleVersion"/>/<see cref="RuleEvaluationResult"/>
/// to their query-side DTOs, by hand rather than through a registered Mapster profile
/// -- the identical deviation <c>ConfigurationMapper</c> states and justifies for the
/// same reason.
/// </summary>
internal static class RuleMapper
{
    public static RuleDefinitionDto ToDto(this RuleDefinition definition) => new(
        definition.Id.Value,
        definition.Key.Value,
        definition.Category,
        definition.Versions.Select(ToDto).ToList());

    public static RuleVersionDto ToDto(this RuleVersion version) => new(
        version.Id.Value,
        version.VersionNumber,
        version.Conditions.Select(ToDto).ToList(),
        version.ConditionOperator.ToString(),
        version.Actions.Select(ToDto).ToList(),
        version.Priority.ToString(),
        version.CreatedByUserId.Value,
        version.State.ToString());

    public static RuleConditionDto ToDto(this RuleCondition condition) =>
        new(condition.FieldName, condition.Operator.ToString(), condition.ComparisonValue);

    public static RuleActionDirectiveDto ToDto(this RuleActionDirective action) =>
        new(action.ActionKey, action.Parameters);

    public static RuleEvaluationResultDto ToDto(this RuleEvaluationResult result) =>
        new(result.IsMatched, result.Actions.Select(ToDto).ToList(), result.FailureReason);
}
