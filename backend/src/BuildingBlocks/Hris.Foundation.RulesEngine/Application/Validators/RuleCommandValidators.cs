using FluentValidation;
using Hris.Foundation.RulesEngine.Application.Commands;
using Hris.Foundation.RulesEngine.Application.Queries;

namespace Hris.Foundation.RulesEngine.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields... Business-
/// independent validation." Deliberately does not re-check anything
/// <see cref="Domain.RuleDefinition"/>'s own factory/transition methods already
/// enforce (key format, lifecycle-order rules, at-least-one-condition/action) -- the
/// identical separation <c>ConfigurationCommandValidators</c> states for its own six.
///
/// Grouped into one file for the same reason
/// <see cref="Domain.RuleVersion"/>'s own five lifecycle handlers are: five of these
/// eight validators are the same two-line "id is not empty" shape, now joined by a
/// scope-id check on every rule-management command, since each one's own handler
/// depends on <c>ScopeLevel</c>/<c>ScopeId</c> to run its authorization check.
/// </summary>
public sealed class CreateRuleDefinitionCommandValidator : AbstractValidator<CreateRuleDefinitionCommand>
{
    public CreateRuleDefinitionCommandValidator()
    {
        RuleFor(c => c.Key).NotEmpty();
        RuleFor(c => c.Category).NotEmpty();
        RuleFor(c => c.CreatedByUserId).NotEmpty();
        RuleFor(c => c.ConditionOperator).IsInEnum();
        RuleFor(c => c.Priority).IsInEnum();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class CreateNewDraftVersionCommandValidator : AbstractValidator<CreateNewDraftVersionCommand>
{
    public CreateNewDraftVersionCommandValidator()
    {
        RuleFor(c => c.RuleDefinitionId).NotEmpty();
        RuleFor(c => c.CreatedByUserId).NotEmpty();
        RuleFor(c => c.ConditionOperator).IsInEnum();
        RuleFor(c => c.Priority).IsInEnum();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class ValidateRuleVersionCommandValidator : AbstractValidator<ValidateRuleVersionCommand>
{
    public ValidateRuleVersionCommandValidator()
    {
        RuleFor(c => c.RuleDefinitionId).NotEmpty();
        RuleFor(c => c.RuleVersionId).NotEmpty();
        RuleFor(c => c.RequestingPrincipalId).NotEmpty();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class PublishRuleVersionCommandValidator : AbstractValidator<PublishRuleVersionCommand>
{
    public PublishRuleVersionCommandValidator()
    {
        RuleFor(c => c.RuleDefinitionId).NotEmpty();
        RuleFor(c => c.RuleVersionId).NotEmpty();
        RuleFor(c => c.RequestingPrincipalId).NotEmpty();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class ActivateRuleVersionCommandValidator : AbstractValidator<ActivateRuleVersionCommand>
{
    public ActivateRuleVersionCommandValidator()
    {
        RuleFor(c => c.RuleDefinitionId).NotEmpty();
        RuleFor(c => c.RuleVersionId).NotEmpty();
        RuleFor(c => c.RequestingPrincipalId).NotEmpty();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class DeprecateRuleVersionCommandValidator : AbstractValidator<DeprecateRuleVersionCommand>
{
    public DeprecateRuleVersionCommandValidator()
    {
        RuleFor(c => c.RuleDefinitionId).NotEmpty();
        RuleFor(c => c.RuleVersionId).NotEmpty();
        RuleFor(c => c.RequestingPrincipalId).NotEmpty();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class ArchiveRuleVersionCommandValidator : AbstractValidator<ArchiveRuleVersionCommand>
{
    public ArchiveRuleVersionCommandValidator()
    {
        RuleFor(c => c.RuleDefinitionId).NotEmpty();
        RuleFor(c => c.RuleVersionId).NotEmpty();
        RuleFor(c => c.RequestingPrincipalId).NotEmpty();
        RuleFor(c => c.ScopeLevel).IsInEnum();
        RuleFor(c => c.ScopeId).NotEmpty();
    }
}

public sealed class EvaluateRuleQueryValidator : AbstractValidator<EvaluateRuleQuery>
{
    public EvaluateRuleQueryValidator()
    {
        RuleFor(q => q.RuleDefinitionId).NotEmpty();
        RuleFor(q => q.Facts).NotNull();
    }
}

public sealed class GetRuleDefinitionByKeyQueryValidator : AbstractValidator<GetRuleDefinitionByKeyQuery>
{
    public GetRuleDefinitionByKeyQueryValidator()
    {
        RuleFor(q => q.Key).NotEmpty();
    }
}
