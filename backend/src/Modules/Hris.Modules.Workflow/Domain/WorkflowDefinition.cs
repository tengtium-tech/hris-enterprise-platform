using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Aggregate Root representing a tenant-authored, versioned specification of what
/// must happen, whose approval, in what order, under what condition, before a
/// specific kind of business request takes effect. Source:
/// docs/04-modules/workflow/domain/aggregates.md, workflow-definitions.md, and
/// workflow-versioning.md.
///
/// It is the template, never the running approval. A specific execution of it for a
/// specific request is a Workflow Engine instance, which this module deliberately
/// does not model.
///
/// The aggregate boundary encloses the steps because publication validates the
/// graph as a whole. <see cref="Publish"/> is where that happens, and it is the
/// load-bearing transition in the module: it rejects far more than it accepts, and
/// once past it a definition never changes under the instances relying on it
/// (WR-004).
/// </summary>
public sealed class WorkflowDefinition : AggregateRoot<WorkflowDefinitionId>
{
    private readonly List<WorkflowStep> _steps = [];

    public Guid TenantId { get; }

    public Guid BusinessProcessId { get; }

    /// <summary>
    /// Shared across every version of this definition. A new version is a new
    /// aggregate instance sharing this identifier, never an in-place edit
    /// (workflow-versioning.md): treating publication as an edit is the natural
    /// first implementation and is wrong in a way that only surfaces months later,
    /// when a policy change silently reaches back into requests already decided.
    /// </summary>
    public Guid LineageId { get; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    /// <summary>
    /// The condition under which this definition applies within its business
    /// process. Whether two published definitions for one business process could
    /// both match a single request is checked at publication from a caller-supplied
    /// signal, since it requires comparing against the tenant's other published
    /// definitions.
    /// </summary>
    public string? TriggerCondition { get; private set; }

    public int Version { get; }

    public DefinitionStatus Status { get; private set; }

    public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    public Guid? PublishedBy { get; private set; }

    public DateTimeOffset? PublishedOn { get; private set; }

    public Guid? DeprecatedBy { get; private set; }

    public DateTimeOffset? DeprecatedOn { get; private set; }

    public string? DeprecationReason { get; private set; }

    private WorkflowDefinition(
        WorkflowDefinitionId id, Guid tenantId, Guid businessProcessId, Guid lineageId, int version, Guid createdBy,
        DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        BusinessProcessId = businessProcessId;
        LineageId = lineageId;
        Version = version;
        Status = DefinitionStatus.Draft;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    /// <summary>
    /// Authors a new definition in <see cref="DefinitionStatus.Draft"/>. A first
    /// version establishes its own lineage, so <see cref="LineageId"/> is the
    /// definition's own identifier.
    ///
    /// <paramref name="nameCollidesWithinBusinessProcess"/> (WR-006) is computed by
    /// the calling Application-layer handler against the tenant's other definitions
    /// for the same business process, a cross-aggregate-instance fact this aggregate
    /// cannot see about itself.
    /// </summary>
    public static Result<WorkflowDefinition> Author(
        WorkflowDefinitionId id, Guid tenantId, Guid businessProcessId, string? name, string? description,
        string? triggerCondition, bool nameCollidesWithinBusinessProcess, Guid createdBy, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkflowDefinition>(WorkflowErrors.DefinitionNameRequired);
        }

        if (nameCollidesWithinBusinessProcess)
        {
            return Result.Failure<WorkflowDefinition>(WorkflowErrors.DefinitionNameNotUniqueForBusinessProcess);
        }

        var definition = new WorkflowDefinition(id, tenantId, businessProcessId, id.Value, 1, createdBy, nowUtc)
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            TriggerCondition = triggerCondition?.Trim(),
        };

        definition.AddDomainEvent(new WorkflowDefinitionCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, businessProcessId, name.Trim(), createdBy));

        return Result.Success(definition);
    }

    /// <summary>
    /// Replaces the draft's entire step set. Editing is expressed as replacement
    /// rather than per-step mutation because the graph's correctness is a property
    /// of the whole set, and a partial edit leaves no meaningful intermediate state
    /// to validate against.
    /// </summary>
    public Result ReplaceSteps(IReadOnlyList<WorkflowStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (Status != DefinitionStatus.Draft)
        {
            return Result.Failure(WorkflowErrors.DefinitionNotDraft);
        }

        _steps.Clear();
        _steps.AddRange(steps);
        return Result.Success();
    }

    public Result AddStep(WorkflowStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (Status != DefinitionStatus.Draft)
        {
            return Result.Failure(WorkflowErrors.DefinitionNotDraft);
        }

        _steps.Add(step);
        return Result.Success();
    }

    public Result RemoveStep(WorkflowStepId stepId)
    {
        if (Status != DefinitionStatus.Draft)
        {
            return Result.Failure(WorkflowErrors.DefinitionNotDraft);
        }

        var step = _steps.Find(candidate => candidate.Id == stepId);
        if (step is null)
        {
            return Result.Failure(WorkflowErrors.StepNotFound);
        }

        _steps.Remove(step);
        return Result.Success();
    }

    public Result Rename(string? name, string? description, string? triggerCondition, bool nameCollidesWithinBusinessProcess)
    {
        if (Status != DefinitionStatus.Draft)
        {
            return Result.Failure(WorkflowErrors.DefinitionNotDraft);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(WorkflowErrors.DefinitionNameRequired);
        }

        if (nameCollidesWithinBusinessProcess)
        {
            return Result.Failure(WorkflowErrors.DefinitionNameNotUniqueForBusinessProcess);
        }

        Name = name.Trim();
        Description = description?.Trim();
        TriggerCondition = triggerCondition?.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Runs the whole-graph validation and makes the definition executable. A single
    /// atomic transition: a definition failing any check is not partially published,
    /// it remains <see cref="DefinitionStatus.Draft"/>, and the specific check that
    /// failed is returned rather than a generic rejection the author would have to
    /// re-derive.
    ///
    /// Two of the checks cannot be made from inside the aggregate and arrive as
    /// caller-supplied signals, the established pattern for a cross-module fact.
    /// <paramref name="anyReferencedCommandMissing"/> is WR-001 and CTR-WFL-001:
    /// whether every automated step's command exists in the named module's public
    /// contract. <paramref name="anyStepCanRouteToRequester"/> is WR-011 and
    /// CTR-WFL-002's publication half: whether any step's rule, or any escalation
    /// target, could statically resolve to the requester, which requires role-holder
    /// data this module does not own. Note that the structural half of CTR-WFL-002
    /// is already unconditional, since <see cref="ApproverResolutionRule"/> has no
    /// field capable of naming an individual at all.
    /// </summary>
    public Result Publish(
        bool anyReferencedCommandMissing, bool anyStepCanRouteToRequester, bool triggerConditionOverlapsAnotherDefinition,
        Guid publishedBy, DateTimeOffset nowUtc)
    {
        if (Status == DefinitionStatus.Published)
        {
            return Result.Failure(WorkflowErrors.DefinitionAlreadyPublished);
        }

        if (Status != DefinitionStatus.Draft)
        {
            return Result.Failure(WorkflowErrors.DefinitionNotDraft);
        }

        var graphResult = ValidateGraph();
        if (graphResult.IsFailure)
        {
            return graphResult;
        }

        if (anyReferencedCommandMissing)
        {
            return Result.Failure(WorkflowErrors.ReferencedCommandDoesNotExist);
        }

        if (anyStepCanRouteToRequester)
        {
            return Result.Failure(WorkflowErrors.DefinitionCanRouteToRequester);
        }

        if (triggerConditionOverlapsAnotherDefinition)
        {
            return Result.Failure(WorkflowErrors.TriggerConditionOverlapsPublishedDefinition);
        }

        Status = DefinitionStatus.Published;
        PublishedBy = publishedBy;
        PublishedOn = nowUtc;

        AddDomainEvent(new WorkflowDefinitionPublished(
            Guid.NewGuid(), nowUtc, Id, TenantId, Version, publishedBy, _steps.Count));

        return Result.Success();
    }

    /// <summary>
    /// Derives the next version as a new aggregate instance sharing this lineage,
    /// starting as a <see cref="DefinitionStatus.Draft"/> copy of this version's
    /// content. Never an edit to this instance: an instance started under this
    /// version must be able to rely on it never changing underneath (WR-050).
    ///
    /// Business process, name, and lineage carry forward; publication and
    /// deprecation history stay with the version they belong to
    /// (workflow-versioning.md's own carry-forward table).
    /// </summary>
    public Result<WorkflowDefinition> CreateNewVersion(WorkflowDefinitionId newId, Guid changedBy, DateTimeOffset nowUtc)
    {
        if (Status != DefinitionStatus.Published)
        {
            return Result.Failure<WorkflowDefinition>(WorkflowErrors.DefinitionNotPublished);
        }

        var next = new WorkflowDefinition(newId, TenantId, BusinessProcessId, LineageId, Version + 1, changedBy, nowUtc)
        {
            Name = Name,
            Description = Description,
            TriggerCondition = TriggerCondition,
        };

        next._steps.AddRange(_steps);

        next.AddDomainEvent(new WorkflowDefinitionVersioned(
            Guid.NewGuid(), nowUtc, newId, TenantId, LineageId, Version, Version + 1, changedBy));

        return Result.Success(next);
    }

    /// <summary>
    /// Withdraws the definition from new instances. Forward-looking only: instances
    /// already running are bound to this version and are never reached by
    /// deprecation, because binding happens once at instance creation and is never
    /// re-evaluated.
    ///
    /// Idempotent by convergence, per commands.md.
    /// </summary>
    public Result Deprecate(Guid deprecatedBy, string? reason, DateTimeOffset nowUtc)
    {
        if (Status == DefinitionStatus.Deprecated)
        {
            return Result.Success();
        }

        if (Status != DefinitionStatus.Published)
        {
            return Result.Failure(WorkflowErrors.DefinitionNotPublished);
        }

        Status = DefinitionStatus.Deprecated;
        DeprecatedBy = deprecatedBy;
        DeprecatedOn = nowUtc;
        DeprecationReason = reason?.Trim();

        AddDomainEvent(new WorkflowDefinitionDeprecated(
            Guid.NewGuid(), nowUtc, Id, TenantId, deprecatedBy, reason?.Trim() ?? string.Empty));

        return Result.Success();
    }

    /// <summary>
    /// WR-002, WR-003, and WR-005: the checks that are properties of the graph
    /// rather than of any single step. Exposed internally so the publication path
    /// and its tests exercise the same code.
    ///
    /// Entry is the lowest-ordered step. entities.md keeps <c>Order</c> "for display
    /// and for the common sequential case" while <c>Successors</c> carries the
    /// actual graph, so order is used only to pick where traversal begins, never to
    /// derive execution sequence, which would collapse every parallel and
    /// conditional structure into a linear chain.
    /// </summary>
    internal Result ValidateGraph()
    {
        if (_steps.Count == 0)
        {
            return Result.Failure(WorkflowErrors.DefinitionHasNoSteps);
        }

        var stepsById = _steps.ToDictionary(step => step.Id.Value);

        foreach (var step in _steps)
        {
            if (step.StepType == StepType.Conditional && step.DefaultSuccessor is null)
            {
                return Result.Failure(WorkflowErrors.ConditionalStepRequiresDefaultSuccessor);
            }

            if (step.StepType != StepType.Terminal && !step.AllTargets().Any())
            {
                return Result.Failure(WorkflowErrors.StepGraphDoesNotTerminate);
            }

            if (step.AllTargets().Any(target => !stepsById.ContainsKey(target)))
            {
                return Result.Failure(WorkflowErrors.StepSuccessorNotFound);
            }
        }

        var entry = _steps.OrderBy(step => step.Order).First();

        if (HasCycle(entry.Id.Value, stepsById))
        {
            return Result.Failure(WorkflowErrors.StepGraphContainsCycle);
        }

        var reachable = Reachable(entry.Id.Value, stepsById);
        return reachable.Count < _steps.Count
            ? Result.Failure(WorkflowErrors.StepGraphContainsUnreachableStep)
            : Result.Success();
    }

    private static bool HasCycle(Guid entryId, Dictionary<Guid, WorkflowStep> stepsById)
    {
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        return Walk(entryId);

        bool Walk(Guid current)
        {
            if (visiting.Contains(current))
            {
                return true;
            }

            if (!visited.Add(current))
            {
                return false;
            }

            visiting.Add(current);

            foreach (var target in stepsById[current].AllTargets())
            {
                if (stepsById.ContainsKey(target) && Walk(target))
                {
                    return true;
                }
            }

            visiting.Remove(current);
            return false;
        }
    }

    private static HashSet<Guid> Reachable(Guid entryId, Dictionary<Guid, WorkflowStep> stepsById)
    {
        var reached = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(entryId);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!reached.Add(current))
            {
                continue;
            }

            foreach (var target in stepsById[current].AllTargets())
            {
                if (stepsById.ContainsKey(target))
                {
                    pending.Push(target);
                }
            }
        }

        return reached;
    }
}
