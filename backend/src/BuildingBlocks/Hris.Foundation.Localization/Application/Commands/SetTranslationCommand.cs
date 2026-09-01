using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Localization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Localization.Application.Commands;

/// <summary>
/// Adds or overwrites one locale's translation on an existing
/// <see cref="TranslationEntry"/>, per localization-framework.md's own "Dynamic
/// Translations... Version Management" -- <see cref="TranslationEntry.SetTranslation"/>
/// increments <see cref="TranslationEntry.VersionNumber"/> on every call, so this is
/// also how a mistranslation gets corrected, not only how a new locale gets added.
///
/// Looked up by <see cref="TranslationEntry.Key"/>, the only lookup
/// <see cref="ITranslationEntryRepository"/> supports (no <c>GetByIdAsync</c>) --
/// the same shape <see cref="CountryConfigurationUpdateCommands"/>'s own five
/// handlers already establish for looking up by <see cref="CountryCode"/> instead of
/// a raw id.
/// </summary>
public sealed record SetTranslationCommand(
    string Key,
    string Locale,
    string Text,
    Guid UpdatedByUserId) : ICommand<Result>;

internal sealed class SetTranslationCommandHandler : IRequestHandler<SetTranslationCommand, Result>
{
    private readonly ITranslationEntryRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SetTranslationCommandHandler(ITranslationEntryRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SetTranslationCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByKeyAsync(request.Key, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            return Result.Failure(LocalizationErrors.TranslationEntryNotFound);
        }

        var localeResult = Domain.Locale.Create(request.Locale);
        if (localeResult.IsFailure)
        {
            return Result.Failure(localeResult.Error);
        }

        return entry.SetTranslation(
            localeResult.Value,
            request.Text,
            new UserAccountId(request.UpdatedByUserId),
            _timeProvider.GetUtcNow());
    }
}
