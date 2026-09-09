using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Identity of the <see cref="ApprovalDelegation"/> Aggregate Root. Deliberately
/// distinct from administration's own <c>DelegationId</c>: the two delegations are
/// different arrangements that both exist (WR-044), and a shared identifier type
/// would invite exactly the merge that document prohibits.
/// </summary>
public readonly record struct ApprovalDelegationId(Guid Value) : IStronglyTypedId;
