namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Part of a <see cref="LeavePolicyRuleset"/>: how balance accrues for the leave type the
/// policy configures. <see cref="Rate"/> and <see cref="Frequency"/> are only meaningful
/// where <see cref="Method"/> is <see cref="AccrualMethod.Periodic"/>. Source:
/// docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public sealed record AccrualRule(
    AccrualMethod Method,
    decimal? Rate,
    AccrualFrequency? Frequency,
    bool ProratesForMidPeriodChange);
