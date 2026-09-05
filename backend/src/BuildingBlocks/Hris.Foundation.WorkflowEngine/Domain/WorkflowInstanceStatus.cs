namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// workflow-engine.md's own Workflow Lifecycle diagram ("Draft -&gt; Submitted -&gt; In
/// Progress -&gt; Pending Approval -&gt; Approved -&gt; Completed") plus its own named
/// "Alternative paths" (Rejected, Cancelled, Expired, Withdrawn, Failed). <c>Draft</c>
/// itself is not a member here -- the same "never an independently observable or
/// persisted state" reasoning <c>Tenant</c>'s own <c>TenantLifecycleState.Provisioning</c>
/// remarks give for its own document's Requested state: a <see cref="WorkflowInstance"/>
/// is not created at all until <see cref="WorkflowInstance.Trigger"/> runs, which
/// produces <see cref="Submitted"/> directly.
/// </summary>
public enum WorkflowInstanceStatus
{
    Submitted = 0,
    InProgress = 1,
    PendingApproval = 2,
    Approved = 3,
    Completed = 4,
    Rejected = 5,
    Cancelled = 6,
    Expired = 7,
    Withdrawn = 8,
    Failed = 9,
}
