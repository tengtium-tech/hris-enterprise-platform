namespace Hris.Foundation.RulesEngine.Application.Dtos;

/// <summary>
/// The read-side shape of a <see cref="Domain.RuleDefinition"/>, per the identical
/// primitive-only reasoning <c>ConfigurationSettingDto</c> already states for its own
/// query-side DTO -- these two frameworks share the same versioned-lifecycle shape,
/// and this DTO mirrors that one field for field.
/// </summary>
public sealed record RuleDefinitionDto(
    Guid Id,
    string Key,
    string Category,
    IReadOnlyList<RuleVersionDto> Versions);

/// <summary>The read-side shape of a <see cref="Domain.RuleVersion"/>.</summary>
public sealed record RuleVersionDto(
    Guid Id,
    int VersionNumber,
    IReadOnlyList<RuleConditionDto> Conditions,
    string ConditionOperator,
    IReadOnlyList<RuleActionDirectiveDto> Actions,
    string Priority,
    Guid CreatedByUserId,
    string State);

/// <summary>The read-side shape of a <see cref="Domain.RuleCondition"/>.</summary>
public sealed record RuleConditionDto(string FieldName, string Operator, string ComparisonValue);

/// <summary>The read-side shape of a <see cref="Domain.RuleActionDirective"/>.</summary>
public sealed record RuleActionDirectiveDto(string ActionKey, IReadOnlyDictionary<string, string> Parameters);
