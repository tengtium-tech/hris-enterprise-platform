using FluentValidation;
using Hris.Foundation.Localization.Application.Commands;
using Hris.Foundation.Localization.Application.Queries;

namespace Hris.Foundation.Localization.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factories/mutation methods already enforce (locale/language/
/// time-zone/country-code/currency format, translation key/text non-empty) -- the
/// identical separation <c>ConfigurationCommandValidators</c> states for its own set.
///
/// Grouped into one file for the same reason <c>RuleCommandValidators</c> is:
/// eight of these nine validators are the same one- or two-line "field is not
/// empty" shape.
/// </summary>
public sealed class CreateCountryConfigurationCommandValidator : AbstractValidator<CreateCountryConfigurationCommand>
{
    public CreateCountryConfigurationCommandValidator()
    {
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.DefaultCurrency).NotEmpty();
        RuleFor(c => c.DefaultLanguage).NotEmpty();
        RuleFor(c => c.DefaultTimeZone).NotEmpty();
        RuleFor(c => c.WorkingDays).NotNull();
        RuleFor(c => c.AddressFormat).NotEmpty();
        RuleFor(c => c.PhoneFormat).NotEmpty();
    }
}

public sealed class UpdateDefaultCurrencyCommandValidator : AbstractValidator<UpdateDefaultCurrencyCommand>
{
    public UpdateDefaultCurrencyCommandValidator()
    {
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.Currency).NotEmpty();
    }
}

public sealed class UpdateDefaultLanguageCommandValidator : AbstractValidator<UpdateDefaultLanguageCommand>
{
    public UpdateDefaultLanguageCommandValidator()
    {
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.Language).NotEmpty();
    }
}

public sealed class UpdateDefaultTimeZoneCommandValidator : AbstractValidator<UpdateDefaultTimeZoneCommand>
{
    public UpdateDefaultTimeZoneCommandValidator()
    {
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.TimeZone).NotEmpty();
    }
}

public sealed class UpdateWorkingDaysCommandValidator : AbstractValidator<UpdateWorkingDaysCommand>
{
    public UpdateWorkingDaysCommandValidator()
    {
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.WorkingDays).NotNull();
    }
}

public sealed class UpdateFormatsCommandValidator : AbstractValidator<UpdateFormatsCommand>
{
    public UpdateFormatsCommandValidator()
    {
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.AddressFormat).NotEmpty();
        RuleFor(c => c.PhoneFormat).NotEmpty();
    }
}

public sealed class CreateTranslationEntryCommandValidator : AbstractValidator<CreateTranslationEntryCommand>
{
    public CreateTranslationEntryCommandValidator()
    {
        RuleFor(c => c.Key).NotEmpty();
        RuleFor(c => c.Locale).NotEmpty();
        RuleFor(c => c.Text).NotEmpty();
        RuleFor(c => c.UpdatedByUserId).NotEmpty();
    }
}

public sealed class SetTranslationCommandValidator : AbstractValidator<SetTranslationCommand>
{
    public SetTranslationCommandValidator()
    {
        RuleFor(c => c.Key).NotEmpty();
        RuleFor(c => c.Locale).NotEmpty();
        RuleFor(c => c.Text).NotEmpty();
        RuleFor(c => c.UpdatedByUserId).NotEmpty();
    }
}

public sealed class GetCountryConfigurationQueryValidator : AbstractValidator<GetCountryConfigurationQuery>
{
    public GetCountryConfigurationQueryValidator()
    {
        RuleFor(q => q.Country).NotEmpty();
    }
}

public sealed class ResolveTranslationQueryValidator : AbstractValidator<ResolveTranslationQuery>
{
    public ResolveTranslationQueryValidator()
    {
        RuleFor(q => q.Key).NotEmpty();
        RuleFor(q => q.Locale).NotEmpty();
    }
}
