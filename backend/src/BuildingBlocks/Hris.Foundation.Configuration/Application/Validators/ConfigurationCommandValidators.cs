using FluentValidation;
using Hris.Foundation.Configuration.Application.Commands;

namespace Hris.Foundation.Configuration.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields, Data
/// formats, Input consistency, Business-independent validation." Deliberately does not
/// re-check anything <see cref="Domain.ConfigurationSetting"/>'s own factory/transition
/// methods already enforce (key format, data-type conformance, lifecycle-order rules)
/// -- coding-standards.md's Application Layer convention separates "validation failure"
/// from "business-rule failure" as different conditions with different HTTP status
/// codes precisely so this layer and the Domain layer are not doing the same check
/// twice for two different reasons.
///
/// Grouped into one file for the same reason
/// <see cref="ConfigurationVersionLifecycleCommands"/>'s handlers are: five of these six
/// validators are the same two-line "id is not empty" shape.
/// </summary>
public sealed class CreateConfigurationSettingCommandValidator : AbstractValidator<CreateConfigurationSettingCommand>
{
    public CreateConfigurationSettingCommandValidator()
    {
        RuleFor(c => c.Key).NotEmpty();
        RuleFor(c => c.InitialValue).NotEmpty();
        RuleFor(c => c.ChangeSummary).NotEmpty();
        RuleFor(c => c.CreatedByUserId).NotEmpty();
        RuleFor(c => c.ScopeId)
            .NotEmpty()
            .When(c => c.ScopeLevel != Domain.ConfigurationScopeLevel.Global)
            .WithMessage("A scope id is required for every scope level except Global.");
    }
}

public sealed class CreateNewDraftVersionCommandValidator : AbstractValidator<CreateNewDraftVersionCommand>
{
    public CreateNewDraftVersionCommandValidator()
    {
        RuleFor(c => c.ConfigurationSettingId).NotEmpty();
        RuleFor(c => c.Value).NotEmpty();
        RuleFor(c => c.ChangeSummary).NotEmpty();
        RuleFor(c => c.CreatedByUserId).NotEmpty();
    }
}

public sealed class ValidateConfigurationVersionCommandValidator : AbstractValidator<ValidateConfigurationVersionCommand>
{
    public ValidateConfigurationVersionCommandValidator()
    {
        RuleFor(c => c.ConfigurationSettingId).NotEmpty();
        RuleFor(c => c.ConfigurationVersionId).NotEmpty();
    }
}

public sealed class ApproveConfigurationVersionCommandValidator : AbstractValidator<ApproveConfigurationVersionCommand>
{
    public ApproveConfigurationVersionCommandValidator()
    {
        RuleFor(c => c.ConfigurationSettingId).NotEmpty();
        RuleFor(c => c.ConfigurationVersionId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
    }
}

public sealed class PublishConfigurationVersionCommandValidator : AbstractValidator<PublishConfigurationVersionCommand>
{
    public PublishConfigurationVersionCommandValidator()
    {
        RuleFor(c => c.ConfigurationSettingId).NotEmpty();
        RuleFor(c => c.ConfigurationVersionId).NotEmpty();
    }
}

public sealed class ActivateConfigurationVersionCommandValidator : AbstractValidator<ActivateConfigurationVersionCommand>
{
    public ActivateConfigurationVersionCommandValidator()
    {
        RuleFor(c => c.ConfigurationSettingId).NotEmpty();
        RuleFor(c => c.ConfigurationVersionId).NotEmpty();
    }
}

public sealed class DeprecateConfigurationVersionCommandValidator : AbstractValidator<DeprecateConfigurationVersionCommand>
{
    public DeprecateConfigurationVersionCommandValidator()
    {
        RuleFor(c => c.ConfigurationSettingId).NotEmpty();
        RuleFor(c => c.ConfigurationVersionId).NotEmpty();
    }
}

public sealed class ArchiveConfigurationVersionCommandValidator : AbstractValidator<ArchiveConfigurationVersionCommand>
{
    public ArchiveConfigurationVersionCommandValidator()
    {
        RuleFor(c => c.ConfigurationSettingId).NotEmpty();
        RuleFor(c => c.ConfigurationVersionId).NotEmpty();
    }
}
