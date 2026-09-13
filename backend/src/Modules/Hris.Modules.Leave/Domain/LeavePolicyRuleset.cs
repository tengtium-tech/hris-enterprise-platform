namespace Hris.Modules.Leave.Domain;

/// <summary>
/// The complete set of answers a <see cref="LeavePolicy"/> version composes — everything
/// request submission, accrual runs, and carryover processing need; an incomplete policy
/// fails validation at publication, never falls back to a hardcoded default at evaluation
/// time. Named <c>Ruleset</c> rather than <c>Configuration</c> to avoid colliding with the
/// EF Core <c>IEntityTypeConfiguration&lt;LeavePolicy&gt;</c> class this module's own
/// Infrastructure layer must also name <c>LeavePolicyConfiguration</c>, the same reason
/// Attendance's own equivalent composite is named <c>PolicyCalculationConfiguration</c>
/// rather than <c>AttendancePolicyConfiguration</c>. Source:
/// docs/04-modules/leave/domain/leave-policies.md.
/// </summary>
public sealed record LeavePolicyRuleset(
    AccrualRule AccrualRule,
    EligibilityCriteria EligibilityCriteria,
    EntitlementCap EntitlementCap,
    CarryoverRule CarryoverRule,
    CommutabilityStatus CommutabilityStatus);
