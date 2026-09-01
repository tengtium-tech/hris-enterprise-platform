using Hris.Application.Abstractions;
using Hris.Foundation.Localization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Localization.Application.Commands;

/// <summary>
/// Creates the one <see cref="CountryConfiguration"/> for a country, per
/// localization-framework.md's Country Configuration section: "Default Currency,
/// Default Language, Time Zone, Working Days, Weekend Definition, Address Format,
/// Phone Format... should be configurable rather than hard-coded." One command per
/// coding-standards.md's Application Layer convention -- the identical shape
/// <c>CreateConfigurationSettingCommand</c> already establishes for a sibling
/// framework's own aggregate creation.
///
/// Carries raw primitives, not Domain Value Objects, across the MediatR boundary --
/// <see cref="CreateCountryConfigurationCommandHandler"/> is the one place a
/// malformed country code, currency, language, or time zone becomes a
/// <see cref="LocalizationErrors"/> failure.
///
/// Not authorization-gated, unlike Rules Engine's own write commands: Authorization
/// Framework is not one of this framework's own stated Upstream Dependencies (only
/// Configuration, Audit, and Logging are), and <c>OrganizationalScopeLevel</c>'s own
/// remarks state it deliberately has no Global level ("a role delegation is always
/// within some tenant") -- there is no scope this platform-wide aggregate could be
/// checked against without inventing a placeholder tenant id, which would be worse
/// than not gating at all.
/// </summary>
public sealed record CreateCountryConfigurationCommand(
    string Country,
    string DefaultCurrency,
    string DefaultLanguage,
    string DefaultTimeZone,
    IReadOnlyCollection<DayOfWeek> WorkingDays,
    string AddressFormat,
    string PhoneFormat) : ICommand<Result<Guid>>;

internal sealed class CreateCountryConfigurationCommandHandler
    : IRequestHandler<CreateCountryConfigurationCommand, Result<Guid>>
{
    private readonly ICountryConfigurationRepository _repository;

    public CreateCountryConfigurationCommandHandler(ICountryConfigurationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<Guid>> Handle(CreateCountryConfigurationCommand request, CancellationToken cancellationToken)
    {
        var countryResult = CountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure<Guid>(countryResult.Error);
        }

        // localization-framework.md's own Country Configuration section describes a
        // country's defaults as singular ("Default Currency, Default Language...")
        // -- two configurations for the same country would make "the" default
        // ambiguous, the same reasoning CreateConfigurationSettingCommandHandler's
        // own key+scope uniqueness check documents.
        if (await _repository.GetByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result.Failure<Guid>(LocalizationErrors.CountryConfigurationAlreadyExists);
        }

        var currencyResult = CurrencyCode.Create(request.DefaultCurrency);
        if (currencyResult.IsFailure)
        {
            return Result.Failure<Guid>(currencyResult.Error);
        }

        var languageResult = LanguageCode.Create(request.DefaultLanguage);
        if (languageResult.IsFailure)
        {
            return Result.Failure<Guid>(languageResult.Error);
        }

        var timeZoneResult = TimeZoneId.Create(request.DefaultTimeZone);
        if (timeZoneResult.IsFailure)
        {
            return Result.Failure<Guid>(timeZoneResult.Error);
        }

        var configurationResult = CountryConfiguration.Create(
            countryResult.Value,
            currencyResult.Value,
            languageResult.Value,
            timeZoneResult.Value,
            request.WorkingDays,
            request.AddressFormat,
            request.PhoneFormat);

        if (configurationResult.IsFailure)
        {
            return Result.Failure<Guid>(configurationResult.Error);
        }

        await _repository.AddAsync(configurationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(configurationResult.Value.Id.Value);
    }
}
