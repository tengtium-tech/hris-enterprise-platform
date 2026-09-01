using Hris.Application.Abstractions;
using Hris.Foundation.Localization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Localization.Application.Commands;

/// <summary>
/// The five update commands over <see cref="CountryConfiguration"/>'s own five
/// mutation methods (<c>UpdateDefaultCurrency</c>, <c>UpdateDefaultLanguage</c>,
/// <c>UpdateDefaultTimeZone</c>, <c>UpdateWorkingDays</c>, <c>UpdateFormats</c>),
/// grouped into one file the same way Rules Engine's own five lifecycle commands are
/// bundled in <c>RuleVersionLifecycleCommands.cs</c>: each handler here is the same
/// three-line shape -- look the aggregate up by <see cref="CountryCode"/> (the only
/// lookup <see cref="ICountryConfigurationRepository"/> supports; there is no
/// <c>GetByIdAsync</c>, per that interface's own remarks that <see cref="CountryConfiguration.Country"/>
/// is "the natural key a repository actually looks up by"), fail with
/// <see cref="LocalizationErrors.CountryConfigurationNotFound"/> if it does not
/// exist, otherwise call the one Domain method and return success. None of the five
/// needs to call <c>AddAsync</c> or any explicit save: the aggregate was already
/// loaded through this same <c>DbContext</c>, so the caller's own
/// <c>TransactionBehavior</c> persists the mutation via change tracking alone.
/// </summary>
public sealed record UpdateDefaultCurrencyCommand(string Country, string Currency) : ICommand<Result>;

internal sealed class UpdateDefaultCurrencyCommandHandler : IRequestHandler<UpdateDefaultCurrencyCommand, Result>
{
    private readonly ICountryConfigurationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateDefaultCurrencyCommandHandler(ICountryConfigurationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateDefaultCurrencyCommand request, CancellationToken cancellationToken)
    {
        var countryResult = CountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure(countryResult.Error);
        }

        var configuration = await _repository.GetByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false);
        if (configuration is null)
        {
            return Result.Failure(LocalizationErrors.CountryConfigurationNotFound);
        }

        var currencyResult = CurrencyCode.Create(request.Currency);
        if (currencyResult.IsFailure)
        {
            return Result.Failure(currencyResult.Error);
        }

        configuration.UpdateDefaultCurrency(currencyResult.Value, _timeProvider.GetUtcNow());
        return Result.Success();
    }
}

public sealed record UpdateDefaultLanguageCommand(string Country, string Language) : ICommand<Result>;

internal sealed class UpdateDefaultLanguageCommandHandler : IRequestHandler<UpdateDefaultLanguageCommand, Result>
{
    private readonly ICountryConfigurationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateDefaultLanguageCommandHandler(ICountryConfigurationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateDefaultLanguageCommand request, CancellationToken cancellationToken)
    {
        var countryResult = CountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure(countryResult.Error);
        }

        var configuration = await _repository.GetByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false);
        if (configuration is null)
        {
            return Result.Failure(LocalizationErrors.CountryConfigurationNotFound);
        }

        var languageResult = LanguageCode.Create(request.Language);
        if (languageResult.IsFailure)
        {
            return Result.Failure(languageResult.Error);
        }

        configuration.UpdateDefaultLanguage(languageResult.Value, _timeProvider.GetUtcNow());
        return Result.Success();
    }
}

public sealed record UpdateDefaultTimeZoneCommand(string Country, string TimeZone) : ICommand<Result>;

internal sealed class UpdateDefaultTimeZoneCommandHandler : IRequestHandler<UpdateDefaultTimeZoneCommand, Result>
{
    private readonly ICountryConfigurationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateDefaultTimeZoneCommandHandler(ICountryConfigurationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateDefaultTimeZoneCommand request, CancellationToken cancellationToken)
    {
        var countryResult = CountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure(countryResult.Error);
        }

        var configuration = await _repository.GetByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false);
        if (configuration is null)
        {
            return Result.Failure(LocalizationErrors.CountryConfigurationNotFound);
        }

        var timeZoneResult = TimeZoneId.Create(request.TimeZone);
        if (timeZoneResult.IsFailure)
        {
            return Result.Failure(timeZoneResult.Error);
        }

        configuration.UpdateDefaultTimeZone(timeZoneResult.Value, _timeProvider.GetUtcNow());
        return Result.Success();
    }
}

public sealed record UpdateWorkingDaysCommand(string Country, IReadOnlyCollection<DayOfWeek> WorkingDays) : ICommand<Result>;

internal sealed class UpdateWorkingDaysCommandHandler : IRequestHandler<UpdateWorkingDaysCommand, Result>
{
    private readonly ICountryConfigurationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateWorkingDaysCommandHandler(ICountryConfigurationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateWorkingDaysCommand request, CancellationToken cancellationToken)
    {
        var countryResult = CountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure(countryResult.Error);
        }

        var configuration = await _repository.GetByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false);
        if (configuration is null)
        {
            return Result.Failure(LocalizationErrors.CountryConfigurationNotFound);
        }

        configuration.UpdateWorkingDays(request.WorkingDays, _timeProvider.GetUtcNow());
        return Result.Success();
    }
}

public sealed record UpdateFormatsCommand(string Country, string AddressFormat, string PhoneFormat) : ICommand<Result>;

internal sealed class UpdateFormatsCommandHandler : IRequestHandler<UpdateFormatsCommand, Result>
{
    private readonly ICountryConfigurationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateFormatsCommandHandler(ICountryConfigurationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateFormatsCommand request, CancellationToken cancellationToken)
    {
        var countryResult = CountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure(countryResult.Error);
        }

        var configuration = await _repository.GetByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false);
        if (configuration is null)
        {
            return Result.Failure(LocalizationErrors.CountryConfigurationNotFound);
        }

        configuration.UpdateFormats(request.AddressFormat, request.PhoneFormat, _timeProvider.GetUtcNow());
        return Result.Success();
    }
}
