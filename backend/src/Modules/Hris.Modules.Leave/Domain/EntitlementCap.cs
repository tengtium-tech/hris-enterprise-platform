namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Part of a <see cref="LeavePolicyRuleset"/>: the maximum balance a leave type may accrue
/// to, and the maximum a single request may draw. For a statutory leave type,
/// <see cref="MaximumAccruable"/> may not be configured below <c>LeaveType.StatutoryMinimum</c>
/// (LV-013). Source: docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public sealed record EntitlementCap(decimal MaximumAccruable, decimal? MaximumPerRequest);
