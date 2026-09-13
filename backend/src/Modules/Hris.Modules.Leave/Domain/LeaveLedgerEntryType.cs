namespace Hris.Modules.Leave.Domain;

/// <summary>
/// The kind of a <see cref="LeaveLedgerEntry"/>. Determines sign convention and which
/// source aggregate is expected to be referenced. Source:
/// docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public enum LeaveLedgerEntryType
{
    /// <summary>Positive. From a scheduled accrual run, per the effective policy's <c>AccrualRule</c>.</summary>
    Accrual,

    /// <summary>
    /// Negative when a <c>LeaveRequest</c> reaches Approved; positive when a compensating
    /// entry restores balance after cancelling an already-deducted request (LV-041) — the
    /// same entry type in both directions, per value-objects.md's own six-member list.
    /// </summary>
    Deduction,

    /// <summary>Positive (a manual grant) or negative (a correction), from an approved <c>LeaveAdjustment</c>.</summary>
    Adjustment,

    /// <summary>Positive. From the scheduled carryover processor, at period end.</summary>
    Carryover,

    /// <summary>Negative. From the scheduled carryover processor, for balance above the carryover cap.</summary>
    Forfeiture,

    /// <summary>Negative. From an approved <c>LeaveEncashment</c>.</summary>
    Encashment,
}
