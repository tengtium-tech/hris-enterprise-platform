using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// Resolves one <see cref="ConfigurationKey"/> across configuration-framework.md's
/// Configuration Hierarchy: "More specific configuration overrides higher-level
/// defaults." A Domain Service per
/// docs/02-architecture/04-domain-driven-design/domain-services.md's Decision Guide
/// ("Does it involve several Aggregates? -&gt; Consider a Domain Service") -- this
/// spans every <see cref="ConfigurationSetting"/> instance sharing the key across the
/// caller's scope chain, not one Aggregate's own behavior.
///
/// The caller supplies every scope it holds explicitly; this type never infers a
/// tenant, company, or department from ambient state, per configuration-framework.md's
/// own Implementation Guidance ("Resolve configuration within tenant context...
/// `CTR-ISO-001`") and `CTR-ISO-003` ("Isolation Enforced Below the Application
/// Layer"). <see cref="ConfigurationScopeLevel"/>'s own declared ordinal -- not the
/// order the caller happens to supply scopes in -- decides which is "more specific,"
/// so a caller cannot accidentally invert precedence by passing scopes out of order.
/// </summary>
public sealed class ConfigurationHierarchyResolver
{
    private readonly IConfigurationSettingRepository _repository;

    public ConfigurationHierarchyResolver(IConfigurationSettingRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    /// <param name="key">The setting being resolved.</param>
    /// <param name="scopeChain">
    /// Every scope applicable to the current request -- e.g. the caller's Global,
    /// Tenant, Company, and Department scopes. Global should always be included as
    /// the ultimate fallback; a chain with no in-force value at any supplied level
    /// resolves to <see cref="ConfigurationErrors.VersionNotFound"/>.
    /// </param>
    public async Task<Result<string>> ResolveAsync(
        ConfigurationKey key,
        IReadOnlyCollection<ConfigurationScope> scopeChain,
        DateOnly asOfDate,
        CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));
        Guard.AgainstNull(scopeChain, nameof(scopeChain));

        var candidates = await _repository
            .FindByKeyAcrossScopesAsync(key, scopeChain, cancellationToken)
            .ConfigureAwait(false);

        var byLevelMostSpecificFirst = candidates
            .OrderByDescending(setting => setting.Scope.Level);

        foreach (var setting in byLevelMostSpecificFirst)
        {
            var resolved = setting.GetValueAsOf(asOfDate);
            if (resolved.IsSuccess)
            {
                return resolved;
            }
        }

        return Result.Failure<string>(ConfigurationErrors.VersionNotFound);
    }
}
