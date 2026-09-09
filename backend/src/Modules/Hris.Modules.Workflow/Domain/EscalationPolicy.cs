using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Governs what happens when an assigned approver does not act. Source:
/// docs/04-modules/workflow/domain/value-objects.md and escalation-and-sla.md.
///
/// Escalation never approves (WR-020). It notifies a new party and, once
/// <see cref="MaxDepth"/> is exhausted, surfaces the stalled request for manual
/// attention. That rule is the one most tempting to relax under operational
/// pressure and the one this type most exists to keep: an approval step exists
/// because a specific person's judgement was required, and auto-approving on
/// timeout would let the operation take effect on nobody's decision.
///
/// <see cref="EscalateTo"/> is an ordinary <see cref="ApproverResolutionRule"/> and
/// carries the identical requester exclusion (WR-022). An implementation enforcing
/// the exclusion on the primary approver but not on escalation has reintroduced
/// self-approval one hop later, under exactly the conditions most likely to make a
/// reviewer accept the outcome without checking who approved it.
/// </summary>
public sealed class EscalationPolicy : ValueObject
{
    /// <summary>
    /// The bound WR-021 requires. An unbounded chain either escalates forever with
    /// nothing surfacing for attention or, worse, keeps re-resolving until it lands
    /// on a role holder who happens to be able to approve, turning escalation into
    /// an unintentional relaxation of who was supposed to decide.
    /// </summary>
    public const int MaximumDepth = 10;

    public TimeSpan TriggerAfter { get; }

    /// <summary>
    /// Assigned by object initializer rather than through the constructor, and so
    /// carrying an internal setter. EF Core rejects a constructor parameter that
    /// binds to an owned navigation, and this property is one: an owned
    /// <see cref="ApproverResolutionRule"/> nested inside this owned policy. The
    /// codebase has now hit that rule five times, but this is the first occurrence on
    /// a Value Object rather than an Entity, and the first at three levels of owned
    /// nesting (definition, step, escalation override, resolution rule). The fix is
    /// the same one every prior occurrence used. Immutability from outside the type
    /// is unaffected: only <see cref="Create"/> assigns it.
    /// </summary>
    public ApproverResolutionRule EscalateTo { get; internal set; } = null!;

    public int MaxDepth { get; }

    private EscalationPolicy(TimeSpan triggerAfter, int maxDepth)
    {
        TriggerAfter = triggerAfter;
        MaxDepth = maxDepth;
    }

    public static Result<EscalationPolicy> Create(TimeSpan triggerAfter, ApproverResolutionRule escalateTo, int maxDepth)
    {
        ArgumentNullException.ThrowIfNull(escalateTo);

        if (triggerAfter <= TimeSpan.Zero)
        {
            return Result.Failure<EscalationPolicy>(WorkflowErrors.EscalationTriggerMustBePositive);
        }

        if (triggerAfter > SlaDuration.PlatformMaximum)
        {
            return Result.Failure<EscalationPolicy>(WorkflowErrors.EscalationTriggerExceedsPlatformMaximum);
        }

        if (maxDepth < 1)
        {
            return Result.Failure<EscalationPolicy>(WorkflowErrors.EscalationDepthMustBePositive);
        }

        return maxDepth > MaximumDepth
            ? Result.Failure<EscalationPolicy>(WorkflowErrors.EscalationDepthUnbounded)
            : Result.Success(new EscalationPolicy(triggerAfter, maxDepth) { EscalateTo = escalateTo });
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TriggerAfter;
        yield return EscalateTo;
        yield return MaxDepth;
    }
}
