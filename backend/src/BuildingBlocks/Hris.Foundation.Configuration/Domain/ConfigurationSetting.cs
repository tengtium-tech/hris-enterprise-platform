using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// Aggregate Root of the Configuration Framework: one logical, scoped setting and
/// every <see cref="ConfigurationVersion"/> ever drafted, published, or retired for
/// it. Source: docs/03-foundation/configuration-framework.md.
///
/// Named <c>ConfigurationSetting</c> rather than the document's own bare
/// "Configuration" -- which is this framework's subject area, not a mandated C#
/// identifier the way <c>Error</c> is error-pattern.md's own literal type name -- per
/// CA1724: the bare name would collide with the legacy <c>System.Configuration</c>
/// namespace.
///
/// Built as the first Sprint 3 Core Kernel framework (IMPLEMENTATION-PLAN.md's
/// bootstrap order), before Authorization, Audit, Event, and Validation Frameworks --
/// its own stated Upstream Dependencies -- exist. Per that plan's own resolution
/// ("a minimal version of each exists before any of them is feature-complete ...
/// wired in incrementally"), this aggregate is deliberately self-contained: it raises
/// its own Domain Events for a future Event Framework to dispatch and a future Audit
/// Framework to subscribe to, but does not reference either. Deeper validation this
/// document calls for ("Dependencies, Cross-Configuration Consistency, Referential
/// Integrity") integrates with the Rules Engine once that framework exists later in
/// this same Sprint; <see cref="ValidateVersion"/> today checks only what this
/// aggregate can determine about itself (required value, data-type conformance,
/// date ordering).
///
/// No Infrastructure/persistence layer exists yet for any Sprint 3 framework
/// (backend/README.md) -- <see cref="IConfigurationSettingRepository"/> is the
/// Domain-owned interface a future EF Core implementation will satisfy.
/// </summary>
public sealed class ConfigurationSetting : AggregateRoot<ConfigurationId>
{
    private readonly List<ConfigurationVersion> _versions = [];

    public ConfigurationKey Key { get; }

    public ConfigurationScope Scope { get; }

    public ConfigurationCategory Category { get; }

    public ConfigurationDataType DataType { get; }

    public IReadOnlyList<ConfigurationVersion> Versions => _versions.AsReadOnly();

    private ConfigurationSetting(ConfigurationId id, ConfigurationKey key, ConfigurationScope scope, ConfigurationCategory category, ConfigurationDataType dataType)
        : base(id)
    {
        Key = key;
        Scope = scope;
        Category = category;
        DataType = dataType;
    }

    public static Result<ConfigurationSetting> Create(
        ConfigurationKey key,
        ConfigurationScope scope,
        ConfigurationCategory category,
        ConfigurationDataType dataType,
        string initialValue,
        DateOnly effectiveDate,
        DateOnly? expirationDate,
        string changeSummary,
        Guid createdByUserId,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(createdByUserId, nameof(createdByUserId));

        var setting = new ConfigurationSetting(new ConfigurationId(Guid.NewGuid()), key, scope, category, dataType);

        var draftResult = setting.CreateDraftVersionCore(initialValue, effectiveDate, expirationDate, changeSummary, createdByUserId);
        if (draftResult.IsFailure)
        {
            return Result.Failure<ConfigurationSetting>(draftResult.Error);
        }

        setting.AddDomainEvent(new ConfigurationCreated(Guid.NewGuid(), nowUtc, setting.Id, key, scope));
        return Result.Success(setting);
    }

    public Result<ConfigurationVersion> CreateNewDraftVersion(
        string value,
        DateOnly effectiveDate,
        DateOnly? expirationDate,
        string changeSummary,
        Guid createdByUserId,
        DateTimeOffset nowUtc)
    {
        var draftResult = CreateDraftVersionCore(value, effectiveDate, expirationDate, changeSummary, createdByUserId);
        if (draftResult.IsFailure)
        {
            return draftResult;
        }

        AddDomainEvent(new ConfigurationUpdated(Guid.NewGuid(), nowUtc, Id, draftResult.Value.Id, draftResult.Value.VersionNumber));
        return draftResult;
    }

    private Result<ConfigurationVersion> CreateDraftVersionCore(
        string value,
        DateOnly effectiveDate,
        DateOnly? expirationDate,
        string changeSummary,
        Guid createdByUserId)
    {
        if (_versions.Any(v => v.State == ConfigurationLifecycleState.Draft))
        {
            return Result.Failure<ConfigurationVersion>(ConfigurationErrors.DraftAlreadyExists);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ConfigurationVersion>(ConfigurationErrors.ValueRequired);
        }

        if (expirationDate is not null && expirationDate.Value < effectiveDate)
        {
            return Result.Failure<ConfigurationVersion>(ConfigurationErrors.ExpirationBeforeEffectiveDate);
        }

        var mostRecentEffectiveDate = _versions
            .Where(v => v.State >= ConfigurationLifecycleState.Published)
            .Select(v => v.EffectiveDate)
            .DefaultIfEmpty()
            .Max();

        if (mostRecentEffectiveDate != default && effectiveDate < mostRecentEffectiveDate)
        {
            return Result.Failure<ConfigurationVersion>(ConfigurationErrors.EffectiveDateBeforePreviousVersion);
        }

        var version = new ConfigurationVersion(
            new ConfigurationVersionId(Guid.NewGuid()),
            _versions.Count + 1,
            value,
            effectiveDate,
            expirationDate,
            changeSummary,
            createdByUserId);

        _versions.Add(version);
        return Result.Success(version);
    }

    /// <summary>
    /// Checks the requirements this aggregate can determine on its own -- required
    /// value, data-type conformance -- per configuration-framework.md's Configuration
    /// Validation section. See this class's own remarks for the deeper checks that
    /// section also names and this method deliberately does not attempt yet.
    /// </summary>
    public Result ValidateVersion(ConfigurationVersionId versionId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(ConfigurationErrors.VersionNotFound);
        }

        if (string.IsNullOrWhiteSpace(version.ChangeSummary))
        {
            return Fail(ConfigurationErrors.ChangeSummaryRequired.Description);
        }

        if (!ValueMatchesDataType(version.Value, DataType))
        {
            return Fail(ConfigurationErrors.ValueDoesNotMatchDataType.Description);
        }

        return version.MarkValidated();

        Result Fail(string reason)
        {
            AddDomainEvent(new ConfigurationValidationFailed(Guid.NewGuid(), nowUtc, Id, versionId, reason));
            return Result.Failure(ConfigurationErrors.ValueDoesNotMatchDataType);
        }
    }

    public Result ApproveVersion(ConfigurationVersionId versionId, Guid approverId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        return version is null
            ? Result.Failure(ConfigurationErrors.VersionNotFound)
            : version.Approve(approverId, nowUtc);
    }

    public Result PublishVersion(ConfigurationVersionId versionId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(ConfigurationErrors.VersionNotFound);
        }

        var result = version.Publish();
        if (result.IsSuccess)
        {
            AddDomainEvent(new ConfigurationPublished(Guid.NewGuid(), nowUtc, Id, versionId, version.VersionNumber, version.EffectiveDate));
        }

        return result;
    }

    public Result ActivateVersion(ConfigurationVersionId versionId, DateOnly asOfDate, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(ConfigurationErrors.VersionNotFound);
        }

        var result = version.Activate(asOfDate);
        if (result.IsSuccess)
        {
            AddDomainEvent(new ConfigurationActivated(Guid.NewGuid(), nowUtc, Id, versionId, version.VersionNumber));
        }

        return result;
    }

    public Result DeprecateVersion(ConfigurationVersionId versionId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(ConfigurationErrors.VersionNotFound);
        }

        var result = version.Deprecate();
        if (result.IsSuccess)
        {
            AddDomainEvent(new ConfigurationDeprecated(Guid.NewGuid(), nowUtc, Id, versionId, version.VersionNumber));
        }

        return result;
    }

    public Result ArchiveVersion(ConfigurationVersionId versionId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(ConfigurationErrors.VersionNotFound);
        }

        var result = version.Archive();
        if (result.IsSuccess)
        {
            AddDomainEvent(new ConfigurationArchived(Guid.NewGuid(), nowUtc, Id, versionId, version.VersionNumber));
        }

        return result;
    }

    /// <summary>
    /// Resolves the value in force on <paramref name="asOfDate"/> from stored
    /// effective/expiration dates alone, never from today's date or from whichever
    /// version currently happens to carry <see cref="ConfigurationLifecycleState.Active"/> --
    /// this is what makes the same query, re-run on any later date, keep returning
    /// the historically correct answer for a past date (`CTR-DAT-005`; also
    /// configuration-framework.md's own Implementation Guidance: "Effective-date
    /// configuration where a change must not alter historical computation").
    /// </summary>
    public Result<string> GetValueAsOf(DateOnly asOfDate)
    {
        var candidate = _versions
            .Where(v => v.IsInForceOn(asOfDate))
            .OrderByDescending(v => v.EffectiveDate)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        return candidate is null
            ? Result.Failure<string>(ConfigurationErrors.VersionNotFound)
            : Result.Success(candidate.Value);
    }

    private ConfigurationVersion? FindVersion(ConfigurationVersionId versionId) =>
        _versions.FirstOrDefault(v => v.Id.Equals(versionId));

    private static bool ValueMatchesDataType(string value, ConfigurationDataType dataType) => dataType switch
    {
        ConfigurationDataType.Text => true,
        ConfigurationDataType.Number => decimal.TryParse(value, out _),
        ConfigurationDataType.Boolean => bool.TryParse(value, out _),
        ConfigurationDataType.Json => IsWellFormedJson(value),
        _ => false,
    };

    private static bool IsWellFormedJson(string value)
    {
        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(value);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
