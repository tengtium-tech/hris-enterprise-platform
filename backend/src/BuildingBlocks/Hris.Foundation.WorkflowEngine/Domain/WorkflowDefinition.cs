using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Aggregate Root holding one workflow template and every
/// <see cref="WorkflowDefinitionVersion"/> ever drafted, published, or deprecated for
/// it, per workflow-engine.md's own Core Concepts and Workflow Versioning sections. The
/// same "config aggregate owning its own versioned child Entities" shape
/// <c>ConfigurationSetting</c> already establishes, not the separate-aggregate-per-version
/// split <c>StatutoryTableVersion</c>'s own remarks justify for itself -- that split
/// exists for population-scale, cross-program history; a workflow definition's own
/// version count is small and bounded to this one aggregate's own consistency boundary
/// (a handful of republications over its lifetime), matching Configuration Framework's
/// own scale, not Statutory Reference Data's.
///
/// <see cref="TenantId"/> is a plain <see cref="Guid"/>, caller-supplied, the same
/// "explicit parameter rather than an ambient tenant-context service" choice
/// <c>IndexedDocument</c>'s own remarks explain -- built concretely here, not deferred,
/// because every canonical participant role this framework's own
/// <see cref="WorkflowStepDefinition.ParticipantRoleName"/> validates against
/// (`../00-project/personas.md`) is itself a tenant-scoped role, and workflow-engine.md's
/// own Permissions table gates "Create workflow definition"/"Publish workflow
/// definition" to tenant-level roles (`HRAdministrator`, `SystemAdministrator`) rather
/// than a platform-wide administrator -- the same tenant-data conclusion
/// <c>Schedule</c>'s and <c>JobQueue</c>'s own remarks reach for their own aggregates,
/// not the platform-owned-data exception <c>StatutoryProgram</c>'s own remarks state
/// for itself.
/// </summary>
public sealed class WorkflowDefinition : AggregateRoot<WorkflowDefinitionId>
{
    private readonly List<WorkflowDefinitionVersion> _versions = [];

    public Guid TenantId { get; }

    public string Name { get; private set; }

    public WorkflowTriggerType TriggerType { get; private set; }

    public string? TriggerExpression { get; private set; }

    public IReadOnlyList<WorkflowDefinitionVersion> Versions => _versions.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; }

    private WorkflowDefinition(WorkflowDefinitionId id, Guid tenantId, string name, WorkflowTriggerType triggerType, string? triggerExpression, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        TriggerType = triggerType;
        TriggerExpression = triggerExpression;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Registers a new workflow definition with its own first
    /// <see cref="WorkflowDefinitionVersion"/> (v1, Draft). Raises no event:
    /// workflow-engine.md's own Domain Events list names no "definition registered"
    /// event, the identical asymmetry <c>JobQueue.Register</c>'s own remarks state for
    /// itself.
    /// </summary>
    public static Result<WorkflowDefinition> Create(
        Guid tenantId,
        string? name,
        WorkflowTriggerType triggerType,
        string? triggerExpression,
        IReadOnlyList<WorkflowStepDefinition> steps,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkflowDefinition>(WorkflowEngineErrors.DefinitionNameRequired);
        }

        var expressionValidation = ValidateTriggerExpression(triggerType, triggerExpression);
        if (expressionValidation.IsFailure)
        {
            return Result.Failure<WorkflowDefinition>(expressionValidation.Error);
        }

        if (steps is null || steps.Count == 0)
        {
            return Result.Failure<WorkflowDefinition>(WorkflowEngineErrors.StepsRequired);
        }

        var definition = new WorkflowDefinition(
            new WorkflowDefinitionId(Guid.NewGuid()), tenantId, name.Trim(), triggerType,
            string.IsNullOrWhiteSpace(triggerExpression) ? null : triggerExpression.Trim(), nowUtc);

        definition._versions.Add(new WorkflowDefinitionVersion(new WorkflowDefinitionVersionId(Guid.NewGuid()), 1, steps, nowUtc));

        return Result.Success(definition);
    }

    /// <summary>
    /// Drafts the next version's own step sequence. Refuses while an unpublished draft
    /// already exists -- the identical
    /// <see cref="WorkflowEngineErrors.DraftAlreadyExists"/> guard
    /// <c>ConfigurationSetting.CreateDraftVersionCore</c>'s own remarks establish for
    /// itself, so there is never more than one editable-but-unpublished version to
    /// confuse a caller about which one <see cref="PublishVersion"/> will act on next.
    /// </summary>
    public Result<WorkflowDefinitionVersion> CreateNewDraftVersion(IReadOnlyList<WorkflowStepDefinition> steps, DateTimeOffset nowUtc)
    {
        if (_versions.Any(v => v.Status == WorkflowDefinitionVersionStatus.Draft))
        {
            return Result.Failure<WorkflowDefinitionVersion>(WorkflowEngineErrors.DraftAlreadyExists);
        }

        if (steps is null || steps.Count == 0)
        {
            return Result.Failure<WorkflowDefinitionVersion>(WorkflowEngineErrors.StepsRequired);
        }

        var version = new WorkflowDefinitionVersion(
            new WorkflowDefinitionVersionId(Guid.NewGuid()), _versions.Count + 1, steps, nowUtc);

        _versions.Add(version);
        return Result.Success(version);
    }

    /// <summary>
    /// Publishes the given Draft version, validating self-approval routing and
    /// canonical role usage across every Approval step it contains
    /// (<see cref="WorkflowDefinitionVersion.Publish"/>). If a different version is
    /// currently <see cref="WorkflowDefinitionVersionStatus.Published"/>, it is
    /// deprecated in the same operation -- a within-aggregate state change, not a
    /// cross-aggregate one, since both versions are child Entities of this same
    /// <see cref="WorkflowDefinition"/>. Instances already running on the deprecated
    /// version are unaffected: <see cref="WorkflowInstance"/> snapshots the version
    /// number it started on at <see cref="WorkflowInstance.Trigger"/> time and never
    /// re-reads this aggregate's own current published version mid-flight.
    /// </summary>
    public Result PublishVersion(int versionNumber, DateTimeOffset nowUtc, IReadOnlySet<string> canonicalParticipantRoleNames)
    {
        Guard.AgainstNull(canonicalParticipantRoleNames, nameof(canonicalParticipantRoleNames));

        var version = FindVersion(versionNumber);
        if (version is null)
        {
            return Result.Failure(WorkflowEngineErrors.VersionNotFound);
        }

        var result = version.Publish(nowUtc, canonicalParticipantRoleNames);
        if (result.IsFailure)
        {
            return result;
        }

        var previouslyPublished = _versions.FirstOrDefault(
            v => v.VersionNumber != versionNumber && v.Status == WorkflowDefinitionVersionStatus.Published);
        previouslyPublished?.Deprecate();

        return Result.Success();
    }

    public Result DeprecateVersion(int versionNumber)
    {
        var version = FindVersion(versionNumber);
        return version is null
            ? Result.Failure(WorkflowEngineErrors.VersionNotFound)
            : version.Deprecate();
    }

    /// <summary>
    /// The version a newly triggered <see cref="WorkflowInstance"/> snapshots and runs
    /// against -- workflow-engine.md's own Workflow Versioning closing statement:
    /// running instances continue on the version they started with, so this is read
    /// once at trigger time, never re-resolved for an instance already in flight.
    /// </summary>
    public WorkflowDefinitionVersion? GetPublishedVersion() =>
        _versions.FirstOrDefault(v => v.Status == WorkflowDefinitionVersionStatus.Published);

    private WorkflowDefinitionVersion? FindVersion(int versionNumber) =>
        _versions.FirstOrDefault(v => v.VersionNumber == versionNumber);

    private static Result ValidateTriggerExpression(WorkflowTriggerType triggerType, string? triggerExpression)
    {
        var requiresExpression = triggerType is WorkflowTriggerType.SystemEvent or WorkflowTriggerType.Scheduled;

        return requiresExpression && string.IsNullOrWhiteSpace(triggerExpression)
            ? Result.Failure(WorkflowEngineErrors.TriggerExpressionRequired)
            : Result.Success();
    }
}
