using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Identifies how an approver is found for an approval step. Source:
/// docs/04-modules/workflow/domain/value-objects.md and the fuller
/// approval-routing.md's "Approver Resolution" section.
///
/// WR-010 and WR-011 are enforced here structurally rather than by a check: this
/// type has no field capable of naming an individual, so a rule that names a person
/// (and therefore a rule that could statically name the requester) is not
/// constructible at all. That is the strongest available form of value-objects.md's
/// own "construction of a rule that would always resolve to the requester's own
/// identity fails outright", and it matches this project's own standing preference
/// for making a rule structurally impossible to violate over documenting it and
/// relying on it being remembered. The complementary dynamic check, excluding the
/// requester from whatever candidate set resolution actually produces, lives in
/// <see cref="ApproverResolution"/>; the two catch different failures and both are
/// required.
///
/// One reconciliation was needed between two documents. value-objects.md's own
/// sketch shows <c>RoleName</c> "present only when Kind = Role" and <c>Scope</c>
/// present for both <see cref="ApproverResolutionKind.ReportingLine"/> and
/// <see cref="ApproverResolutionKind.OrganizationalScope"/>. approval-routing.md,
/// the deep dive, is more specific on both points: an organizational-scope rule
/// "resolves to whoever holds a specified role at a specified organizational scope"
/// (so it needs a role name as well as a scope), and a reporting-line rule
/// "resolves against the requester's manager chain at runtime; it holds no stored
/// target" (so it needs neither). The deep dive's own more detailed statement is
/// followed here, the same way this platform resolved Employee's own lifecycle
/// stages against that module's looser diagram.
/// </summary>
public sealed class ApproverResolutionRule : ValueObject
{
    public ApproverResolutionKind Kind { get; }

    /// <summary>
    /// The role whose holders may approve. Required for
    /// <see cref="ApproverResolutionKind.Role"/> and
    /// <see cref="ApproverResolutionKind.OrganizationalScope"/>; absent for
    /// <see cref="ApproverResolutionKind.ReportingLine"/>. A plain string rather than
    /// a typed reference into administration: whether the role exists, canonical or
    /// tenant-defined, is validated by the caller against that module's own public
    /// contract, since no module takes a compile-time reference on a sibling.
    /// </summary>
    public string? RoleName { get; }

    /// <summary>
    /// The organizational scope the role is held at, as a plain identifier into the
    /// organization module. Required for
    /// <see cref="ApproverResolutionKind.OrganizationalScope"/> only.
    /// </summary>
    public Guid? ScopeId { get; }

    private ApproverResolutionRule(ApproverResolutionKind kind, string? roleName, Guid? scopeId)
    {
        Kind = kind;
        RoleName = roleName;
        ScopeId = scopeId;
    }

    /// <summary>Resolves to whoever currently holds the named role, as a shared queue across all holders.</summary>
    public static Result<ApproverResolutionRule> ForRole(string? roleName)
    {
        return string.IsNullOrWhiteSpace(roleName)
            ? Result.Failure<ApproverResolutionRule>(WorkflowErrors.RoleResolutionRequiresRoleName)
            : Result.Success(new ApproverResolutionRule(ApproverResolutionKind.Role, roleName.Trim(), null));
    }

    /// <summary>
    /// Resolves to the requester's manager at runtime. Holds no stored target, which
    /// is what lets one definition serve every requester as the organization changes.
    /// </summary>
    public static Result<ApproverResolutionRule> ForReportingLine() =>
        Result.Success(new ApproverResolutionRule(ApproverResolutionKind.ReportingLine, null, null));

    /// <summary>Resolves to holders of <paramref name="roleName"/> at <paramref name="scopeId"/>.</summary>
    public static Result<ApproverResolutionRule> ForOrganizationalScope(string? roleName, Guid? scopeId)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Result.Failure<ApproverResolutionRule>(WorkflowErrors.RoleResolutionRequiresRoleName);
        }

        return scopeId is null
            ? Result.Failure<ApproverResolutionRule>(WorkflowErrors.ScopeResolutionRequiresScope)
            : Result.Success(new ApproverResolutionRule(ApproverResolutionKind.OrganizationalScope, roleName.Trim(), scopeId));
    }

    /// <summary>
    /// Whether this rule can produce an empty candidate set once the requester is
    /// excluded, making the top-of-hierarchy exemption reachable. True for every
    /// kind: reporting-line resolution finds nobody where the requester has no
    /// manager, and role or scope resolution finds nobody where the requester is the
    /// sole holder at the relevant scope (approval-routing.md).
    /// </summary>
    public static bool CanResolveToEmptySet => true;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return RoleName;
        yield return ScopeId;
    }

    public override string ToString() => Kind switch
    {
        ApproverResolutionKind.Role => $"Role:{RoleName}",
        ApproverResolutionKind.OrganizationalScope => $"Scope:{RoleName}@{ScopeId}",
        _ => "ReportingLine",
    };
}
