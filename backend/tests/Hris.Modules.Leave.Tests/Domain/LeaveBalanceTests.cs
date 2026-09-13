using FluentAssertions;
using Hris.Modules.Leave.Domain;
using Xunit;

namespace Hris.Modules.Leave.Tests.Domain;

/// <summary>
/// LV-020, LV-021: the module's most consequential invariant — <see cref="LeaveBalance.CurrentBalance"/>
/// is never directly written; every change is an insert-only <see cref="LeaveLedgerEntry"/>, and the
/// total must always equal the ledger's own sum (CTR-DAT-006).
/// </summary>
public sealed class LeaveBalanceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly LeaveTypeId _leaveTypeId = new(Guid.NewGuid());

    private LeaveBalance NewBalance() =>
        LeaveBalance.Create(new LeaveBalanceId(Guid.NewGuid()), _tenantId, _employeeId, _leaveTypeId).Value;

    private static void AssertReproducible(LeaveBalance balance) =>
        balance.CurrentBalance.Should().Be(
            balance.LedgerEntries.Sum(e => e.Amount), "LV-021: the current balance must always equal the ledger's own sum");

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsAtZero_WithNoLedgerEntries()
    {
        var balance = NewBalance();

        balance.CurrentBalance.Should().Be(0);
        balance.LedgerEntries.Should().BeEmpty();
    }

    [Fact]
    public void Create_Fails_WhenEmployeeIdIsEmpty()
    {
        var result = LeaveBalance.Create(new LeaveBalanceId(Guid.NewGuid()), _tenantId, Guid.Empty, _leaveTypeId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EmployeeIdentifierRequired);
    }

    // ---- RecordAccrual (LV-024) -------------------------------------------

    [Fact]
    public void RecordAccrual_AppendsAnEntry_AndRaisesAccruedEvent()
    {
        var balance = NewBalance();
        var sourceReference = Guid.NewGuid();

        var result = balance.RecordAccrual(sourceReference, 1.25m, TestLeave.Today, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(1.25m);
        balance.LedgerEntries.Should().ContainSingle(e =>
            e.EntryType == LeaveLedgerEntryType.Accrual && e.Amount == 1.25m && e.SourceReference == sourceReference && e.Actor == null);
        balance.DomainEvents.OfType<LeaveBalanceAccrued>().Should().ContainSingle();
        balance.DomainEvents.OfType<LeaveLedgerEntryAppended>().Should().ContainSingle();
        AssertReproducible(balance);
    }

    [Fact]
    public void RecordAccrual_IsIdempotent_ForTheSameEffectiveDate()
    {
        // LV-024: a re-run for a period already accrued must not duplicate.
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 1.25m, TestLeave.Today, TestLeave.NowUtc);
        balance.ClearDomainEvents();

        var result = balance.RecordAccrual(Guid.NewGuid(), 1.25m, TestLeave.Today, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.LedgerEntries.Should().ContainSingle("a duplicate accrual for the same period must be skipped, not appended");
        balance.CurrentBalance.Should().Be(1.25m);
        balance.DomainEvents.Should().BeEmpty("skipping a duplicate must not re-raise the event a real accrual already raised once");
    }

    [Fact]
    public void RecordAccrual_Fails_WhenAmountIsNotPositive()
    {
        var balance = NewBalance();

        var result = balance.RecordAccrual(Guid.NewGuid(), 0m, TestLeave.Today, TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LedgerEntryAmountMustBePositive);
    }

    // ---- ApplyDeduction / ApplyCompensatingEntry (LV-022, LV-040, LV-041) --------

    [Fact]
    public void ApplyDeduction_Succeeds_WhenSufficientBalanceIsAvailable()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 5m, TestLeave.Today, TestLeave.NowUtc);
        var requestId = Guid.NewGuid();

        var result = balance.ApplyDeduction(requestId, 3m, TestLeave.Today, Guid.NewGuid(), allowNegativeBalance: false, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(2m);
        balance.LedgerEntries.Should().Contain(e => e.EntryType == LeaveLedgerEntryType.Deduction && e.Amount == -3m);
        AssertReproducible(balance);
    }

    [Fact]
    public void ApplyDeduction_Fails_WhenItWouldDriveBalanceNegative_AndPolicyDoesNotPermitIt()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 2m, TestLeave.Today, TestLeave.NowUtc);

        var result = balance.ApplyDeduction(Guid.NewGuid(), 3m, TestLeave.Today, Guid.NewGuid(), allowNegativeBalance: false, TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.InsufficientBalance);
        balance.CurrentBalance.Should().Be(2m, "a rejected deduction must not partially apply");
    }

    [Fact]
    public void ApplyDeduction_Succeeds_WhenNegativeBalanceIsExplicitlyPermitted()
    {
        // LV-022: an advance-leave provision on the effective policy, resolved by the caller.
        var balance = NewBalance();

        var result = balance.ApplyDeduction(Guid.NewGuid(), 3m, TestLeave.Today, Guid.NewGuid(), allowNegativeBalance: true, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(-3m);
    }

    [Fact]
    public void ApplyCompensatingEntry_RestoresBalance_WithoutEditingTheOriginalDeduction()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 5m, TestLeave.Today, TestLeave.NowUtc);
        balance.ApplyDeduction(Guid.NewGuid(), 3m, TestLeave.Today, Guid.NewGuid(), allowNegativeBalance: false, TestLeave.NowUtc);

        var result = balance.ApplyCompensatingEntry(Guid.NewGuid(), 3m, TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(5m);
        balance.LedgerEntries.Should().HaveCount(3, "the original deduction is retained, never edited (LV-020)");
        balance.LedgerEntries.Where(e => e.EntryType == LeaveLedgerEntryType.Deduction).Should().HaveCount(2);
        AssertReproducible(balance);
    }

    // ---- RecordAdjustment (LV-052, LV-054) ---------------------------------

    [Fact]
    public void RecordAdjustment_Succeeds_ForAPositiveGrant()
    {
        var balance = NewBalance();

        var result = balance.RecordAdjustment(Guid.NewGuid(), 2m, TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(2m);
    }

    [Fact]
    public void RecordAdjustment_Succeeds_ForANegativeCorrection_WhenBalanceRemainsNonNegative()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 5m, TestLeave.Today, TestLeave.NowUtc);

        var result = balance.RecordAdjustment(Guid.NewGuid(), -2m, TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(3m);
    }

    [Fact]
    public void RecordAdjustment_Fails_WhenANegativeCorrectionWouldDriveBalanceNegative()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 1m, TestLeave.Today, TestLeave.NowUtc);

        var result = balance.RecordAdjustment(Guid.NewGuid(), -2m, TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.InsufficientBalance);
    }

    [Fact]
    public void RecordAdjustment_Fails_WhenAmountIsZero()
    {
        var balance = NewBalance();

        var result = balance.RecordAdjustment(Guid.NewGuid(), 0m, TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LedgerEntryAmountMustNotBeZero);
    }

    // ---- RecordEncashment (LV-071) ------------------------------------

    [Fact]
    public void RecordEncashment_Succeeds_WhenSufficientBalanceIsAvailable()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 5m, TestLeave.Today, TestLeave.NowUtc);

        var result = balance.RecordEncashment(Guid.NewGuid(), 2m, TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(3m);
    }

    [Fact]
    public void RecordEncashment_Fails_WhenBalanceIsInsufficient()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 1m, TestLeave.Today, TestLeave.NowUtc);

        var result = balance.RecordEncashment(Guid.NewGuid(), 2m, TestLeave.Today, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.InsufficientBalance);
    }

    // ---- RecordCarryover / RecordForfeiture (LV-060, LV-061) -------------

    [Fact]
    public void RecordCarryover_AppendsAPositiveEntry_WithNoActor_AndRaisesCarriedOverEvent()
    {
        var balance = NewBalance();

        var result = balance.RecordCarryover(Guid.NewGuid(), 5.5m, TestLeave.Today, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(5.5m);
        balance.LedgerEntries.Should().ContainSingle(e => e.EntryType == LeaveLedgerEntryType.Carryover && e.Actor == null);
        balance.DomainEvents.OfType<LeaveCarriedOver>().Should().ContainSingle();
    }

    [Fact]
    public void RecordForfeiture_AppendsANegativeEntry_WithNoActor_AndRaisesForfeitedEvent()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 10m, TestLeave.Today, TestLeave.NowUtc);

        var result = balance.RecordForfeiture(Guid.NewGuid(), 4m, TestLeave.Today, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(6m);
        balance.LedgerEntries.Should().Contain(e => e.EntryType == LeaveLedgerEntryType.Forfeiture && e.Amount == -4m && e.Actor == null);
        balance.DomainEvents.OfType<LeaveForfeited>().Should().ContainSingle();
    }

    // ---- Recalculate (LV-025) ----------------------------------------

    [Fact]
    public void Recalculate_ReproducesTheSameTotal_AfterSeveralOperations_AndRaisesTheEvent()
    {
        var balance = NewBalance();
        balance.RecordAccrual(Guid.NewGuid(), 5m, TestLeave.Today, TestLeave.NowUtc);
        balance.ApplyDeduction(Guid.NewGuid(), 2m, TestLeave.Today, Guid.NewGuid(), allowNegativeBalance: false, TestLeave.NowUtc);
        var actorId = Guid.NewGuid();
        var balanceBeforeRecalculation = balance.CurrentBalance;

        var result = balance.Recalculate(actorId, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(balanceBeforeRecalculation, "resumming an undrifted ledger must reproduce the identical total (CTR-DAT-006)");
        balance.LastRecalculatedAt.Should().Be(TestLeave.NowUtc);
        var raised = balance.DomainEvents.OfType<LeaveBalanceRecalculated>().Single();
        raised.ActorId.Should().Be(actorId);
        raised.WasCorrected.Should().BeFalse("the aggregate's own public API never lets the maintained total drift from the ledger");
    }

    [Fact]
    public void Recalculate_SucceedsOnAFreshBalanceWithNoEntries()
    {
        var balance = NewBalance();

        var result = balance.Recalculate(Guid.NewGuid(), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        balance.CurrentBalance.Should().Be(0);
    }
}
