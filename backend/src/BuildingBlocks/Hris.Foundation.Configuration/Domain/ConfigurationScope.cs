using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// Where in configuration-framework.md's Configuration Hierarchy ("Global -&gt;
/// Tenant -&gt; Company -&gt; Legal Entity -&gt; Business Unit -&gt; Department -&gt;
/// Individual Override") one <see cref="ConfigurationSetting"/> instance sits.
/// <see cref="ScopeId"/> identifies the concrete tenant, company, department, and so
/// on at that level -- deliberately a raw <see cref="Guid"/> rather than a strongly
/// typed <c>TenantId</c>/<c>DepartmentId</c>, since which aggregate that id belongs
/// to is determined entirely by <see cref="Level"/>, and the Configuration Framework
/// has no dependency on Tenant, Organization, or any other module's own identity
/// types (`CTR-ARC-002`) -- those modules do not exist until later Phases.
/// </summary>
public sealed class ConfigurationScope : ValueObject
{
    public ConfigurationScopeLevel Level { get; }

    public Guid? ScopeId { get; }

    private ConfigurationScope(ConfigurationScopeLevel level, Guid? scopeId)
    {
        Level = level;
        ScopeId = scopeId;
    }

    public static Result<ConfigurationScope> Create(ConfigurationScopeLevel level, Guid? scopeId)
    {
        if (level == ConfigurationScopeLevel.Global)
        {
            return scopeId is not null
                ? Result.Failure<ConfigurationScope>(ConfigurationErrors.ScopeIdNotAllowedForGlobalLevel)
                : Result.Success(new ConfigurationScope(level, null));
        }

        if (scopeId is null || scopeId == Guid.Empty)
        {
            return Result.Failure<ConfigurationScope>(ConfigurationErrors.ScopeIdRequiredForNonGlobalLevel);
        }

        return Result.Success(new ConfigurationScope(level, scopeId));
    }

    public static ConfigurationScope Global() => new(ConfigurationScopeLevel.Global, null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Level;
        yield return ScopeId;
    }

    public override string ToString() => ScopeId is null ? Level.ToString() : $"{Level}:{ScopeId}";
}
