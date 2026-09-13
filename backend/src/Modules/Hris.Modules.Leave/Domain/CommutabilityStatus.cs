namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Part of a <see cref="LeavePolicyRuleset"/>, read by <c>LeaveEncashment</c>: whether and
/// how much of the balance under this policy may be converted to pay (LV-070). Source:
/// docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public sealed record CommutabilityStatus(bool IsCommutable, decimal? MaximumCommutable);
