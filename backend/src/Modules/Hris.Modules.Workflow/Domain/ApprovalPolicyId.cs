using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Identity of the <see cref="ApprovalPolicy"/> Aggregate Root.
/// </summary>
public readonly record struct ApprovalPolicyId(Guid Value) : IStronglyTypedId;
