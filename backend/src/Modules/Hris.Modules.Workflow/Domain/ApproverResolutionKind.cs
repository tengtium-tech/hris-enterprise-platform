namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// The three ways an approver may be found. Source:
/// docs/04-modules/workflow/domain/approval-routing.md's "Approver Resolution"
/// section.
///
/// There is deliberately no <c>NamedPerson</c> kind (WR-010). A rule naming a
/// specific user is correct on the day it is authored and wrong the day that person
/// changes role, goes on leave, or leaves the tenant, at which point the definition
/// either routes to someone with no standing to approve or stops with nothing to
/// reassign.
/// </summary>
public enum ApproverResolutionKind
{
    /// <summary>
    /// Whoever currently holds the named role at a scope covering the request,
    /// offered to every holder as a shared queue rather than one arbitrarily chosen
    /// holder.
    /// </summary>
    Role = 0,

    /// <summary>The requester's manager, sourced from employment's reporting line.</summary>
    ReportingLine = 1,

    /// <summary>
    /// Whoever holds a role at a specified organizational scope covering the
    /// requester's placement, sourced from organization.
    /// </summary>
    OrganizationalScope = 2,
}
