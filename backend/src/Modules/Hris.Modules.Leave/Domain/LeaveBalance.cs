using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// An employee's balance for one leave type, as an append-only ledger with a maintained
/// running total. The module's most consequential design decision (README.md,
/// aggregates.md): <see cref="CurrentBalance"/> is never directly written by any command —
/// every change is recorded as an insert-only <see cref="LeaveLedgerEntry"/>, and the total
/// must always be exactly re-derivable by summing that set (LV-021, CTR-DAT-006). Source:
/// docs/04-modules/leave/domain/aggregates.md, leave-balances.md.
///
/// No entry type is ever written from within another aggregate's own transaction — every
/// entry-producing method here is called from this aggregate's own transaction, triggered
/// by an event a different aggregate raised (a request's approval, an adjustment's
/// approval, a scheduled job), never invoked directly as part of that other aggregate's own
/// commit.
/// </summary>
public sealed class LeaveBalance : AggregateRoot<LeaveBalanceId>
{
    private readonly List<LeaveLedgerEntry> _ledgerEntries = [];

    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    /// <summary>A maintained total; always equal to the sum of <see cref="LedgerEntries"/> (LV-021).</summary>
    public decimal CurrentBalance { get; private set; }

    public DateTimeOffset? LastRecalculatedAt { get; private set; }

    public IReadOnlyList<LeaveLedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    private LeaveBalance(LeaveBalanceId id, Guid tenantId, Guid employeeId, LeaveTypeId leaveTypeId)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        CurrentBalance = 0m;
    }

    public static Result<LeaveBalance> Create(LeaveBalanceId id, Guid tenantId, Guid employeeId, LeaveTypeId leaveTypeId)
    {
        if (employeeId == Guid.Empty)
        {
            return Result.Failure<LeaveBalance>(LeaveErrors.EmployeeIdentifierRequired);
        }

        return Result.Success(new LeaveBalance(id, tenantId, employeeId, leaveTypeId));
    }

    /// <summary>
    /// Appends an Accrual entry per the effective policy's <c>AccrualRule</c>. Idempotent
    /// per employee per period (LV-024): a re-run for a period already accrued detects the
    /// existing entry for that effective date and skips rather than duplicates.
    /// </summary>
    public Result RecordAccrual(Guid sourceReference, decimal amount, DateOnly effectiveDate, DateTimeOffset recordedOnUtc)
    {
        if (amount <= 0)
        {
            return Result.Failure(LeaveErrors.LedgerEntryAmountMustBePositive);
        }

        if (_ledgerEntries.Any(e => e.EntryType == LeaveLedgerEntryType.Accrual && e.EffectiveDate == effectiveDate))
        {
            return Result.Success();
        }

        AppendEntry(LeaveLedgerEntryType.Accrual, amount, effectiveDate, sourceReference, null, recordedOnUtc);
        AddDomainEvent(new LeaveBalanceAccrued(Guid.NewGuid(), recordedOnUtc, Id, TenantId, amount, effectiveDate));
        return Result.Success();
    }

    /// <summary>
    /// Deducts balance for a <c>LeaveRequest</c> reaching Approved (LV-040). Rejected if it
    /// would drive the balance negative unless <paramref name="allowNegativeBalance"/> is
    /// true — resolved by the caller from the effective <c>LeavePolicy</c>'s own
    /// advance-leave provision (LV-022), the same reason LV-013's statutory floor is
    /// resolved by a command handler rather than read here directly (Aggregate Design
    /// Rule 13). Ordinarily the caller has already routed any excess to Leave Without Pay
    /// (LV-086) before this is ever called with an amount that would trip this guard.
    /// </summary>
    public Result ApplyDeduction(
        Guid sourceReference, decimal amount, DateOnly effectiveDate, Guid actorId, bool allowNegativeBalance,
        DateTimeOffset recordedOnUtc)
    {
        if (amount <= 0)
        {
            return Result.Failure(LeaveErrors.LedgerEntryAmountMustBePositive);
        }

        if (!allowNegativeBalance && CurrentBalance - amount < 0)
        {
            return Result.Failure(LeaveErrors.InsufficientBalance);
        }

        AppendEntry(LeaveLedgerEntryType.Deduction, -amount, effectiveDate, sourceReference, actorId, recordedOnUtc);
        return Result.Success();
    }

    /// <summary>
    /// Restores balance after cancelling an already-deducted Approved request (LV-041).
    /// Recorded as a positive <see cref="LeaveLedgerEntryType.Deduction"/> entry — the
    /// original deduction is never edited or deleted (LV-020); this is a new, offsetting
    /// entry referencing it.
    /// </summary>
    public Result ApplyCompensatingEntry(
        Guid sourceReference, decimal amount, DateOnly effectiveDate, Guid actorId, DateTimeOffset recordedOnUtc)
    {
        if (amount <= 0)
        {
            return Result.Failure(LeaveErrors.LedgerEntryAmountMustBePositive);
        }

        AppendEntry(LeaveLedgerEntryType.Deduction, amount, effectiveDate, sourceReference, actorId, recordedOnUtc);
        return Result.Success();
    }

    /// <summary>
    /// Applies an approved <c>LeaveAdjustment</c> (LV-052). <paramref name="amount"/> may be
    /// positive (a manual grant) or negative (a correction) per leave-adjustments.md; a
    /// negative amount is still rejected if it would drive the balance below zero.
    /// </summary>
    public Result RecordAdjustment(
        Guid sourceReference, decimal amount, DateOnly effectiveDate, Guid actorId, DateTimeOffset recordedOnUtc)
    {
        if (amount == 0)
        {
            return Result.Failure(LeaveErrors.LedgerEntryAmountMustNotBeZero);
        }

        if (amount < 0 && CurrentBalance + amount < 0)
        {
            return Result.Failure(LeaveErrors.InsufficientBalance);
        }

        AppendEntry(LeaveLedgerEntryType.Adjustment, amount, effectiveDate, sourceReference, actorId, recordedOnUtc);
        return Result.Success();
    }

    /// <summary>Applies an approved <c>LeaveEncashment</c> (LV-071). Commutability and sufficiency are validated at the encashment's own submission (LV-070); this still defends the invariant directly.</summary>
    public Result RecordEncashment(
        Guid sourceReference, decimal amount, DateOnly effectiveDate, Guid actorId, DateTimeOffset recordedOnUtc)
    {
        if (amount <= 0)
        {
            return Result.Failure(LeaveErrors.LedgerEntryAmountMustBePositive);
        }

        if (CurrentBalance - amount < 0)
        {
            return Result.Failure(LeaveErrors.InsufficientBalance);
        }

        AppendEntry(LeaveLedgerEntryType.Encashment, -amount, effectiveDate, sourceReference, actorId, recordedOnUtc);
        return Result.Success();
    }

    /// <summary>
    /// Appends a Carryover entry at a period boundary (LV-060). No actor — a scheduled run,
    /// never a person's decision (LV-092).
    /// </summary>
    public Result RecordCarryover(Guid sourceReference, decimal amount, DateOnly effectiveDate, DateTimeOffset recordedOnUtc)
    {
        if (amount <= 0)
        {
            return Result.Failure(LeaveErrors.LedgerEntryAmountMustBePositive);
        }

        AppendEntry(LeaveLedgerEntryType.Carryover, amount, effectiveDate, sourceReference, null, recordedOnUtc);
        AddDomainEvent(new LeaveCarriedOver(Guid.NewGuid(), recordedOnUtc, Id, TenantId, amount, effectiveDate));
        return Result.Success();
    }

    /// <summary>
    /// Appends a Forfeiture entry for balance above the carryover cap, after any configured
    /// grace period (LV-061). Same run as <see cref="RecordCarryover"/>; no actor (LV-092).
    /// </summary>
    public Result RecordForfeiture(Guid sourceReference, decimal amount, DateOnly effectiveDate, DateTimeOffset recordedOnUtc)
    {
        if (amount <= 0)
        {
            return Result.Failure(LeaveErrors.LedgerEntryAmountMustBePositive);
        }

        AppendEntry(LeaveLedgerEntryType.Forfeiture, -amount, effectiveDate, sourceReference, null, recordedOnUtc);
        AddDomainEvent(new LeaveForfeited(Guid.NewGuid(), recordedOnUtc, Id, TenantId, amount, effectiveDate));
        return Result.Success();
    }

    /// <summary>
    /// Re-derives <see cref="CurrentBalance"/> by resumming the full ledger (LV-025,
    /// CTR-DAT-006). A non-destructive verification and repair operation, invoked
    /// explicitly and audited by an actor — never a side effect of an ordinary balance
    /// read, and never system-initiated the way Accrual/Carryover/Forfeiture are. Always
    /// raises <see cref="LeaveBalanceRecalculated"/>, whether or not the total actually
    /// changed: confirming no drift is itself the auditable outcome.
    /// </summary>
    public Result Recalculate(Guid actorId, DateTimeOffset recalculatedOnUtc)
    {
        var resummed = _ledgerEntries.Sum(e => e.Amount);
        var wasCorrected = resummed != CurrentBalance;
        CurrentBalance = resummed;
        LastRecalculatedAt = recalculatedOnUtc;
        AddDomainEvent(new LeaveBalanceRecalculated(Guid.NewGuid(), recalculatedOnUtc, Id, TenantId, actorId, wasCorrected));
        return Result.Success();
    }

    private void AppendEntry(
        LeaveLedgerEntryType entryType, decimal amount, DateOnly effectiveDate, Guid sourceReference, Guid? actor,
        DateTimeOffset recordedOnUtc)
    {
        var entry = new LeaveLedgerEntry(
            new LeaveLedgerEntryId(Guid.NewGuid()), entryType, amount, effectiveDate, sourceReference, actor, recordedOnUtc);
        _ledgerEntries.Add(entry);
        CurrentBalance += amount;
        AddDomainEvent(new LeaveLedgerEntryAppended(Guid.NewGuid(), recordedOnUtc, Id, TenantId, entry.Id, entryType, amount));
    }
}
