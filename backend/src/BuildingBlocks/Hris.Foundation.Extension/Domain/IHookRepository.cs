namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="Hook"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface IHookRepository
{
    Task<Hook?> GetByIdAsync(HookId id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Hook>> GetByExtensionPointIdAsync(ExtensionPointId extensionPointId, CancellationToken cancellationToken);

    Task AddAsync(Hook hook, CancellationToken cancellationToken);
}
