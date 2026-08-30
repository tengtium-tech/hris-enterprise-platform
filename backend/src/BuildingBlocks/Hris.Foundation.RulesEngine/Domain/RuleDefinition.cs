using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// Aggregate Root of the Rules Engine: one named business policy and every
/// <see cref="RuleVersion"/> ever drafted, published, or retired for it. Source:
/// docs/03-foundation/rules-engine.md.
///
/// Built after Authorization and Audit Frameworks in Sprint 3's bootstrap order, so
/// <see cref="UserAccountId"/> is used directly rather than a raw Guid placeholder.
/// <see cref="Category"/> is an open string, not the five-value enum its own "typical
/// categories" (Payroll, Attendance, Leave, Recruitment, Performance) might suggest:
/// this document's Downstream Consumers list eight more modules (Benefits,
/// Compensation, Succession, Workforce Planning, and others) that will each need
/// their own category, and "typical" in the source document's own wording already
/// signals the list is illustrative.
/// </summary>
public sealed class RuleDefinition : AggregateRoot<RuleDefinitionId>
{
    private readonly List<RuleVersion> _versions = [];

    public RuleKey Key { get; }

    public string Category { get; }

    public IReadOnlyList<RuleVersion> Versions => _versions.AsReadOnly();

    private RuleDefinition(RuleDefinitionId id, RuleKey key, string category)
        : base(id)
    {
        Key = key;
        Category = category;
    }

    public static Result<RuleDefinition> Create(
        RuleKey key,
        string? category,
        IReadOnlyCollection<RuleCondition> conditions,
        LogicalOperator conditionOperator,
        IReadOnlyCollection<RuleActionDirective> actions,
        RulePriority priority,
        UserAccountId createdByUserId,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(key, nameof(key));

        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure<RuleDefinition>(RuleErrors.CategoryRequired);
        }

        var definition = new RuleDefinition(new RuleDefinitionId(Guid.NewGuid()), key, category.Trim());

        var draftResult = definition.CreateDraftVersionCore(conditions, conditionOperator, actions, priority, createdByUserId);
        if (draftResult.IsFailure)
        {
            return Result.Failure<RuleDefinition>(draftResult.Error);
        }

        definition.AddDomainEvent(new RuleCreated(Guid.NewGuid(), nowUtc, definition.Id, key, definition.Category));
        return Result.Success(definition);
    }

    public Result<RuleVersion> CreateNewDraftVersion(
        IReadOnlyCollection<RuleCondition> conditions,
        LogicalOperator conditionOperator,
        IReadOnlyCollection<RuleActionDirective> actions,
        RulePriority priority,
        UserAccountId createdByUserId,
        DateTimeOffset nowUtc)
    {
        var draftResult = CreateDraftVersionCore(conditions, conditionOperator, actions, priority, createdByUserId);
        if (draftResult.IsFailure)
        {
            return draftResult;
        }

        AddDomainEvent(new RuleUpdated(Guid.NewGuid(), nowUtc, Id, draftResult.Value.Id, draftResult.Value.VersionNumber));
        return draftResult;
    }

    private Result<RuleVersion> CreateDraftVersionCore(
        IReadOnlyCollection<RuleCondition> conditions,
        LogicalOperator conditionOperator,
        IReadOnlyCollection<RuleActionDirective> actions,
        RulePriority priority,
        UserAccountId createdByUserId)
    {
        if (_versions.Any(v => v.State == RuleLifecycleState.Draft))
        {
            return Result.Failure<RuleVersion>(RuleErrors.DraftAlreadyExists);
        }

        var version = new RuleVersion(
            new RuleVersionId(Guid.NewGuid()),
            _versions.Count + 1,
            conditions,
            conditionOperator,
            actions,
            priority,
            createdByUserId);

        _versions.Add(version);
        return Result.Success(version);
    }

    public Result ValidateVersion(RuleVersionId versionId)
    {
        var version = FindVersion(versionId);
        return version is null ? Result.Failure(RuleErrors.VersionNotFound) : version.MarkValidated();
    }

    public Result PublishVersion(RuleVersionId versionId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(RuleErrors.VersionNotFound);
        }

        var result = version.Publish();
        if (result.IsSuccess)
        {
            AddDomainEvent(new RulePublished(Guid.NewGuid(), nowUtc, Id, versionId, version.VersionNumber));
        }

        return result;
    }

    public Result ActivateVersion(RuleVersionId versionId)
    {
        var version = FindVersion(versionId);
        return version is null ? Result.Failure(RuleErrors.VersionNotFound) : version.Activate();
    }

    public Result DeprecateVersion(RuleVersionId versionId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(RuleErrors.VersionNotFound);
        }

        var result = version.Deprecate();
        if (result.IsSuccess)
        {
            AddDomainEvent(new RuleDeprecated(Guid.NewGuid(), nowUtc, Id, versionId));
        }

        return result;
    }

    public Result ArchiveVersion(RuleVersionId versionId, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionId);
        if (version is null)
        {
            return Result.Failure(RuleErrors.VersionNotFound);
        }

        var result = version.Archive();
        if (result.IsSuccess)
        {
            AddDomainEvent(new RuleArchived(Guid.NewGuid(), nowUtc, Id, versionId));
        }

        return result;
    }

    /// <summary>
    /// The single version this rule currently evaluates against.
    /// rules-engine.md does not describe multiple simultaneously Active versions the
    /// way effective-dated configuration can have several Published versions
    /// covering different date ranges -- a rule instead has exactly one governing
    /// version at a time, superseded by deprecating it and activating the next.
    /// </summary>
    public Result<RuleVersion> GetActiveVersion()
    {
        var active = _versions.SingleOrDefault(v => v.State == RuleLifecycleState.Active);
        return active is null
            ? Result.Failure<RuleVersion>(RuleErrors.NoActiveVersion)
            : Result.Success(active);
    }

    private RuleVersion? FindVersion(RuleVersionId versionId) => _versions.FirstOrDefault(v => v.Id.Equals(versionId));
}
