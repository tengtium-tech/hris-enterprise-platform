namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="ExtensionPoint"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface IExtensionPointRepository
{
    Task<ExtensionPoint?> GetByIdAsync(ExtensionPointId id, CancellationToken cancellationToken);

    Task<ExtensionPoint?> GetByKeyAsync(ExtensionPointKey key, CancellationToken cancellationToken);

    Task<bool> ExistsByKeyAsync(ExtensionPointKey key, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExtensionPoint>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(ExtensionPoint extensionPoint, CancellationToken cancellationToken);
}
