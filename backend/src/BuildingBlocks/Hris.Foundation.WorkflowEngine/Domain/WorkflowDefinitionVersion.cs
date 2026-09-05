using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// One version of a <see cref="WorkflowDefinition"/>'s own step sequence, per
/// workflow-engine.md's own Workflow Versioning section ("Draft Version, Published
/// Version, Deprecated Version") and its own closing statement: "Running workflow
/// instances should continue using the version from which they were created."
///
/// A child Entity of the <see cref="WorkflowDefinition"/> Aggregate, never an Aggregate
/// Root of its own -- the identical shape and reasoning
/// <c>ConfigurationVersion</c>'s own remarks already establish for its sibling
/// versioned child Entity: version count per definition is small and bounded (a
/// handful of republications over a definition's own lifetime), unlike the genuinely
/// population-scale occurrence aggregates this Sprint's own siblings
/// (<c>WorkflowInstance</c>, <c>WorkflowTask</c>) are. Its constructor and every
/// transition method are <c>internal</c>, reachable only through
/// <see cref="WorkflowDefinition"/>'s own methods, never called directly from outside
/// this assembly.
/// </summary>
public sealed class WorkflowDefinitionVersion : Entity<WorkflowDefinitionVersionId>
{
    public int VersionNumber { get; }

    public IReadOnlyList<WorkflowStepDefinition> Steps { get; }

    public WorkflowDefinitionVersionStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    internal WorkflowDefinitionVersion(
        WorkflowDefinitionVersionId id,
        int versionNumber,
        IReadOnlyList<WorkflowStepDefinition> steps,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        VersionNumber = versionNumber;
        Steps = steps;
        Status = WorkflowDefinitionVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Validates the self-approval and canonical-role invariants workflow-engine.md's
    /// own Permissions section requires be checked "at publication," then transitions
    /// this version -- <see cref="WorkflowDefinition.PublishVersion"/> is the only
    /// caller, and it deprecates any previously published sibling version once this one
    /// succeeds.
    /// </summary>
    internal Result Publish(DateTimeOffset nowUtc, IReadOnlySet<string> canonicalParticipantRoleNames)
    {
        if (Status != WorkflowDefinitionVersionStatus.Draft)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidVersionLifecycleTransition);
        }

        foreach (var step in Steps)
        {
            if (step.StepType != WorkflowStepType.Approval)
            {
                continue;
            }

            if (step.ParticipantType == WorkflowParticipantType.DynamicRequester)
            {
                return Result.Failure(WorkflowEngineErrors.SelfApprovalRoutingNotAllowed);
            }

            if (step.ParticipantType == WorkflowParticipantType.Role
                && (step.ParticipantRoleName is null || !canonicalParticipantRoleNames.Contains(step.ParticipantRoleName)))
            {
                return Result.Failure(WorkflowEngineErrors.InvalidParticipantRoleName);
            }
        }

        Status = WorkflowDefinitionVersionStatus.Published;
        PublishedAtUtc = nowUtc;
        return Result.Success();
    }

    internal Result Deprecate()
    {
        if (Status != WorkflowDefinitionVersionStatus.Published)
        {
            return Result.Failure(WorkflowEngineErrors.InvalidVersionLifecycleTransition);
        }

        Status = WorkflowDefinitionVersionStatus.Deprecated;
        return Result.Success();
    }
}
