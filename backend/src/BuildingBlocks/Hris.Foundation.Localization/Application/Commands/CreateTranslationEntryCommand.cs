using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Localization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Localization.Application.Commands;

/// <summary>
/// Creates a new <see cref="TranslationEntry"/> with its first locale/text pair, per
/// localization-framework.md's Translation Management section ("Translation Keys...
/// Dynamic Translations"). <see cref="TranslationEntry.Create"/> itself requires one
/// locale and text upfront (it calls its own <c>SetTranslation</c> internally), so
/// this command's shape mirrors that -- a translation entry with zero translations
/// is not a state this aggregate's own factory can produce.
///
/// Not authorization-gated for the same structural reason
/// <see cref="CreateCountryConfigurationCommand"/>'s own remarks give: Authorization
/// Framework is not one of this framework's own stated Upstream Dependencies, and
/// <c>OrganizationalScopeLevel</c> has no Global level to check a platform-wide
/// translation catalog against.
/// </summary>
public sealed record CreateTranslationEntryCommand(
    string Key,
    string Locale,
    string Text,
    Guid UpdatedByUserId) : ICommand<Result<Guid>>;

internal sealed class CreateTranslationEntryCommandHandler
    : IRequestHandler<CreateTranslationEntryCommand, Result<Guid>>
{
    private readonly ITranslationEntryRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateTranslationEntryCommandHandler(ITranslationEntryRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateTranslationEntryCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return Result.Failure<Guid>(LocalizationErrors.TranslationKeyRequired);
        }

        // localization-framework.md's own Translation Management section describes
        // one entry per key ("Translation Keys... Version Management") -- two
        // entries for the same key would make "the" translation for that key
        // ambiguous, the same reasoning CreateRuleDefinitionCommandHandler's own
        // key uniqueness check documents.
        if (await _repository.GetByKeyAsync(request.Key.Trim(), cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result.Failure<Guid>(LocalizationErrors.TranslationKeyAlreadyExists);
        }

        var localeResult = Locale.Create(request.Locale);
        if (localeResult.IsFailure)
        {
            return Result.Failure<Guid>(localeResult.Error);
        }

        var entryResult = TranslationEntry.Create(
            request.Key,
            localeResult.Value,
            request.Text,
            new UserAccountId(request.UpdatedByUserId),
            _timeProvider.GetUtcNow());

        if (entryResult.IsFailure)
        {
            return Result.Failure<Guid>(entryResult.Error);
        }

        await _repository.AddAsync(entryResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(entryResult.Value.Id.Value);
    }
}
