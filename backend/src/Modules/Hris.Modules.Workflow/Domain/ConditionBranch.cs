namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// One (predicate, target) pair within a <see cref="StepCondition"/>. Source:
/// docs/04-modules/workflow/domain/value-objects.md's own StepCondition sketch.
///
/// A plain record with no identity of its own: branches are ordered content of the
/// condition, not addressable children with their own lifecycle. That shape is why
/// <see cref="StepCondition.Branches"/> is persisted as a single JSON-serialized
/// column rather than an owned collection, matching the pattern the Administration
/// module established for <c>DelegatedAuthorityItem</c>.
/// </summary>
/// <param name="Predicate">
/// The test evaluated against data the invoking module's public contract exposes.
/// Never another module's internals (WR-001).
/// </param>
/// <param name="TargetStepId">The step taken when this predicate is the first to match.</param>
public sealed record ConditionBranch(string Predicate, Guid TargetStepId);
