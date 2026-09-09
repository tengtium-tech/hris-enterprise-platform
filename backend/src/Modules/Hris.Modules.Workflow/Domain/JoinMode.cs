namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// How a convergence point treats its several predecessors. Source:
/// docs/04-modules/workflow/domain/approval-routing.md's "Parallel" table.
///
/// Present on a step only where it is the convergence point of more than one
/// predecessor; a step reached from a single predecessor carrying a join mode has
/// nothing to join and fails construction (entities.md).
/// </summary>
public enum JoinMode
{
    /// <summary>
    /// Every parallel branch must complete before the join proceeds. Typical use is
    /// dual sign-off. A rejection on any branch fails the join immediately, since
    /// the outcome can no longer become an approval.
    /// </summary>
    All = 0,

    /// <summary>
    /// The first branch to reach a decision proceeds and the remainder are
    /// cancelled. A single rejection does not fail the join while other branches
    /// remain pending; the join resolves on the first decision of either kind.
    /// </summary>
    Any = 1,
}
