using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// The Workflow Module's only entity, owned by <see cref="WorkflowDefinition"/>.
/// Source: docs/04-modules/workflow/domain/entities.md.
///
/// A step has identity, order, configuration, and its own approver resolution rule,
/// which resembles an Aggregate Root but is not one: a definition is validated and
/// published as a whole, and publication asks questions no individual step can
/// answer (does the chain terminate, is any step unreachable, does the sequence
/// contain a cycle, is every conditional branch covered). Each is a property of the
/// graph, not of a node. Were steps separately persistable, a definition could be
/// published valid and then edited step by step into an invalid one with no point
/// at which the whole was checked. There is deliberately no step repository
/// (CTR-ARC-004).
///
/// Every owned value <see cref="ApproverResolutionRule"/>, <see cref="CommandReference"/>,
/// <see cref="StepCondition"/>, <see cref="EscalationOverride"/>, and
/// <see cref="SlaOverride"/> is an init-assigned property with an internal setter
/// rather than a constructor parameter. EF Core rejects a constructor parameter
/// that binds to an owned navigation ("No suitable constructor was found ... cannot
/// bind"), an issue this codebase has now hit on five separate aggregates; the
/// object-initializer shape is the established fix. The setter is internal rather
/// than private because <see cref="Create"/> assigns from this same class while
/// <see cref="WorkflowDefinition"/> reads them.
/// </summary>
public sealed class WorkflowStep : Entity<WorkflowStepId>
{
    public int Order { get; private set; }

    public StepType StepType { get; }

    public string Name { get; private set; } = null!;

    public ApproverResolutionRule? ApproverResolutionRule { get; internal set; }

    public CommandReference? CommandReference { get; internal set; }

    public StepCondition? Condition { get; internal set; }

    /// <summary>
    /// One successor for a sequential or automated step; several for a parallel
    /// fan-out or a conditional step's branch targets. Persisted as a single
    /// JSON-serialized column: these are identifiers, not entities with their own
    /// lifecycle, the same shape <c>DelegatedAuthorityItem</c> established.
    /// </summary>
    public IReadOnlyList<Guid> Successors { get; internal set; } = [];

    /// <summary>
    /// Required on every conditional step, checked here at construction as well as
    /// in the whole-graph check at publication (WR-003). The double check is
    /// deliberate: construction catches the defect the moment a step is authored
    /// without one, and publication catches it where steps were assembled or
    /// imported by a path that bypassed construction.
    /// </summary>
    public Guid? DefaultSuccessor { get; private set; }

    /// <summary>
    /// Present only where this step converges more than one predecessor. Whether it
    /// does is a property of the graph, so the check that a join mode is legitimate
    /// lives in <see cref="WorkflowDefinition"/>'s publication validation; what this
    /// entity enforces is the narrower half it can see, that a step declared with a
    /// join mode is not simultaneously declared terminal.
    /// </summary>
    public JoinMode? JoinMode { get; private set; }

    /// <summary>Where true, no <see cref="ApprovalDelegation"/> satisfies this step (WR-012).</summary>
    public bool NonDelegable { get; private set; }

    public EscalationPolicy? EscalationOverride { get; internal set; }

    public SlaDuration? SlaOverride { get; internal set; }

    private WorkflowStep(WorkflowStepId id, int order, StepType stepType)
        : base(id)
    {
        Order = order;
        StepType = stepType;
    }

    /// <summary>
    /// Enforces entities.md's own presence rules as construction invariants rather
    /// than leaving them as nullable fields to validate later: each optional
    /// configuration is present if and only if the step type requires it.
    /// </summary>
    public static Result<WorkflowStep> Create(
        WorkflowStepId id, int order, StepType stepType, string? name, ApproverResolutionRule? approverResolutionRule,
        CommandReference? commandReference, StepCondition? condition, IReadOnlyList<Guid>? successors,
        Guid? defaultSuccessor, JoinMode? joinMode, bool nonDelegable, EscalationPolicy? escalationOverride,
        SlaDuration? slaOverride)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkflowStep>(WorkflowErrors.StepNameRequired);
        }

        if ((stepType == StepType.Approval) != (approverResolutionRule is not null))
        {
            return Result.Failure<WorkflowStep>(WorkflowErrors.ApprovalStepRequiresResolutionRule);
        }

        if ((stepType == StepType.Automated) != (commandReference is not null))
        {
            return Result.Failure<WorkflowStep>(WorkflowErrors.AutomatedStepRequiresCommandReference);
        }

        if ((stepType == StepType.Conditional) != (condition is not null))
        {
            return Result.Failure<WorkflowStep>(WorkflowErrors.ConditionalStepRequiresCondition);
        }

        var resolvedSuccessors = successors?.ToList() ?? [];

        if (stepType == StepType.Terminal && (resolvedSuccessors.Count > 0 || defaultSuccessor is not null))
        {
            return Result.Failure<WorkflowStep>(WorkflowErrors.TerminalStepCannotHaveSuccessors);
        }

        if (stepType == StepType.Conditional && defaultSuccessor is null)
        {
            return Result.Failure<WorkflowStep>(WorkflowErrors.ConditionalStepRequiresDefaultSuccessor);
        }

        if (stepType == StepType.Terminal && joinMode is not null)
        {
            return Result.Failure<WorkflowStep>(WorkflowErrors.JoinModeRequiresMultiplePredecessors);
        }

        var step = new WorkflowStep(id, order, stepType)
        {
            Name = name.Trim(),
            ApproverResolutionRule = approverResolutionRule,
            CommandReference = commandReference,
            Condition = condition,
            Successors = resolvedSuccessors,
            DefaultSuccessor = defaultSuccessor,
            JoinMode = joinMode,
            NonDelegable = nonDelegable,
            EscalationOverride = escalationOverride,
            SlaOverride = slaOverride,
        };

        return Result.Success(step);
    }

    /// <summary>
    /// Every target this step can hand control to: its declared successors plus, for
    /// a conditional step, the default path. Used by the definition's own graph
    /// validation, which is the only place able to see whether these resolve.
    /// </summary>
    public IEnumerable<Guid> AllTargets()
    {
        foreach (var successor in Successors)
        {
            yield return successor;
        }

        if (DefaultSuccessor is not null)
        {
            yield return DefaultSuccessor.Value;
        }
    }
}
