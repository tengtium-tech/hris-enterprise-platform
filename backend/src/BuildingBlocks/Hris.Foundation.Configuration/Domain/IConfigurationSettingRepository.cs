using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="ConfigurationSetting"/> Aggregate Root,
/// per docs/02-architecture/04-domain-driven-design/repositories.md: "Repository
/// interfaces belong in the Domain layer... Implementation in Infrastructure."
/// Named after the Aggregate Root it owns, per that document's own Repository Naming
/// section.
///
/// No Infrastructure implementation exists yet -- no EF Core model exists for any
/// Sprint 3 framework (backend/README.md) -- this interface is what that
/// implementation will satisfy. Every method takes its scope explicitly rather than
/// reading it from an ambient/thread-static context, so tenant isolation is a
/// property of the method signature an implementation cannot get wrong by omission
/// (`CTR-ISO-003`).
/// </summary>
public interface IConfigurationSettingRepository
{
    Task<ConfigurationSetting?> GetByIdAsync(ConfigurationId id, CancellationToken cancellationToken);

    Task<ConfigurationSetting?> GetByKeyAndScopeAsync(ConfigurationKey key, ConfigurationScope scope, CancellationToken cancellationToken);

    /// <summary>
    /// Every <see cref="ConfigurationSetting"/> sharing <paramref name="key"/> across
    /// exactly the scopes in <paramref name="candidateScopes"/> -- the set
    /// <see cref="ConfigurationHierarchyResolver"/> needs to resolve one value across
    /// the hierarchy in a single round trip, rather than one query per level.
    /// </summary>
    Task<IReadOnlyList<ConfigurationSetting>> FindByKeyAcrossScopesAsync(
        ConfigurationKey key,
        IReadOnlyCollection<ConfigurationScope> candidateScopes,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(ConfigurationKey key, ConfigurationScope scope, CancellationToken cancellationToken);

    Task AddAsync(ConfigurationSetting setting, CancellationToken cancellationToken);
}
