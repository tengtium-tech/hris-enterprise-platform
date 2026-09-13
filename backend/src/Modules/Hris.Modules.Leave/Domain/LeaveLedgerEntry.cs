using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// One insert-only entry in a <see cref="LeaveBalance"/>'s ledger. A child entity, never
/// its own root — no external caller ever asks for "ledger entry 8814" directly; every
/// legitimate access pattern asks for the ledger, or the balance, not one entry in
/// isolation (CTR-ARC-004). Source: docs/04-modules/leave/domain/entities.md
/// (LeaveLedgerEntry).
///
/// Once written, never edited or deleted (LV-020) — enforced here by exposing no method
/// that could mutate one after construction, and at the schema level by granting the
/// owning table no <c>UPDATE</c> permission (infrastructure/persistence.md). A correction
/// to a mistaken entry is always a new, offsetting entry referencing the one it corrects,
/// never an edit in place.
/// </summary>
public sealed class LeaveLedgerEntry : Entity<LeaveLedgerEntryId>
{
    public LeaveLedgerEntryType EntryType { get; }

    /// <summary>Signed, in the leave type's configured unit (days or hours).</summary>
    public decimal Amount { get; }

    /// <summary>The date this entry is attributed to, which may differ from <see cref="RecordedAt"/>.</summary>
    public DateOnly EffectiveDate { get; }

    /// <summary>
    /// The id of the <c>LeaveRequest</c>, <c>LeaveAdjustment</c>, <c>LeaveEncashment</c>, or
    /// carryover run that caused this entry (LV-023). Never absent.
    /// </summary>
    public Guid SourceReference { get; }

    /// <summary>Null for a system-initiated entry — a scheduled accrual or carryover run (LV-092).</summary>
    public Guid? Actor { get; }

    public DateTimeOffset RecordedAt { get; }

    internal LeaveLedgerEntry(
        LeaveLedgerEntryId id, LeaveLedgerEntryType entryType, decimal amount, DateOnly effectiveDate,
        Guid sourceReference, Guid? actor, DateTimeOffset recordedAt)
        : base(id)
    {
        EntryType = entryType;
        Amount = amount;
        EffectiveDate = effectiveDate;
        SourceReference = sourceReference;
        Actor = actor;
        RecordedAt = recordedAt;
    }
}
