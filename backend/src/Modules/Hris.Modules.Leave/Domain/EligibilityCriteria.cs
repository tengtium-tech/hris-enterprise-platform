namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Part of a <see cref="LeavePolicyRuleset"/>: conditions an employment must meet before a
/// <c>LeaveRequest</c> against the policy's leave type is accepted. Employment status and
/// type are referenced as plain strings — this module holds no compile-time reference to
/// <c>employment</c>'s own enumerations (CTR-ARC-002). Source:
/// docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public sealed record EligibilityCriteria(
    int MinimumTenureMonths,
    IReadOnlyList<string> EligibleEmploymentStatuses,
    IReadOnlyList<string> EligibleEmploymentTypes);
