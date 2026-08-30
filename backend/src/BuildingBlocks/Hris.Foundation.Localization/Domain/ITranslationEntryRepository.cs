namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// Persistence abstraction for <see cref="TranslationEntry"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split. No
/// Infrastructure implementation exists yet (backend/README.md).
/// </summary>
public interface ITranslationEntryRepository
{
    Task<TranslationEntry?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    Task AddAsync(TranslationEntry entry, CancellationToken cancellationToken);
}
