namespace Hris.Modules.Leave.Domain;

/// <summary>
/// A <see cref="LeavePolicy"/> version's lifecycle stage. <see cref="Superseded"/> is a
/// distinct state (not merely an Active row with a past <c>EffectiveTo</c>) per
/// docs/04-modules/leave/infrastructure/persistence.md's own Status column. Source:
/// docs/04-modules/leave/domain/leave-policies.md.
/// </summary>
public enum LeavePolicyStatus
{
    Draft,
    Active,
    Superseded,
    Retired,
}
