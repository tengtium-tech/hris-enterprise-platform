using FluentAssertions;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

public sealed class WorkShiftTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private Result<WorkShift> Create(
        string? code = "DAY", string? name = "Day Shift", ShiftTiming? timing = null, bool isOvernight = false,
        WorkDateAnchorRule? anchorRule = null, IReadOnlyList<ShiftPeriod>? splitPeriods = null,
        IReadOnlyList<BreakRule>? breakRules = null, bool codeInUse = false) =>
        WorkShift.Create(
            new WorkShiftId(Guid.NewGuid()), _tenantId, code, name, timing ?? TestTimekeeping.FixedTiming(),
            isOvernight, anchorRule, splitPeriods, breakRules, true, PremiumEligibilityFlags.None,
            TestTimekeeping.Today, codeInUse, Guid.NewGuid(), TestTimekeeping.NowUtc);

    /// <summary>TK-020, both halves. The rule must be present exactly when it can apply.</summary>
    [Fact]
    public void Create_Fails_WhenAnOvernightShiftHasNoAnchorRule()
    {
        Create(isOvernight: true).Error.Should().Be(TimekeepingErrors.OvernightShiftRequiresAnchorRule);
    }

    [Fact]
    public void Create_Fails_WhenANonOvernightShiftCarriesAnAnchorRule()
    {
        Create(isOvernight: false, anchorRule: WorkDateAnchorRule.AnchoredToShiftStart).Error
            .Should().Be(TimekeepingErrors.AnchorRuleProhibitedForNonOvernightShift);
    }

    [Fact]
    public void Create_Succeeds_ForAnOvernightShiftWithAnAnchorRule()
    {
        var result = Create(isOvernight: true, anchorRule: WorkDateAnchorRule.AnchoredToShiftStart);

        result.IsSuccess.Should().BeTrue();
        result.Value.AnchorRule.Should().NotBeNull();
    }

    [Fact]
    public void Create_Fails_WhenCodeIsMissingOrAlreadyUsed()
    {
        Create(code: "  ").Error.Should().Be(TimekeepingErrors.ShiftCodeRequired);
        Create(codeInUse: true).Error.Should().Be(TimekeepingErrors.ShiftCodeNotUniqueWithinTenant);
    }

    [Fact]
    public void Create_Fails_WhenNameIsMissing()
    {
        Create(name: " ").Error.Should().Be(TimekeepingErrors.WorkShiftNameRequired);
    }

    /// <summary>TK-021.</summary>
    [Fact]
    public void Create_Fails_WhenSplitPeriodsOverlap()
    {
        IReadOnlyList<ShiftPeriod> periods =
        [
            new(1, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            new(2, new TimeOnly(11, 0), new TimeOnly(15, 0)),
        ];

        Create(splitPeriods: periods).Error.Should().Be(TimekeepingErrors.SplitShiftPeriodsOverlap);
    }

    [Fact]
    public void Create_Succeeds_ForNonOverlappingSplitPeriods()
    {
        IReadOnlyList<ShiftPeriod> periods =
        [
            new(1, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            new(2, new TimeOnly(13, 0), new TimeOnly(17, 0)),
        ];

        Create(splitPeriods: periods).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Fails_WhenASplitShiftHasOnlyOnePeriod()
    {
        IReadOnlyList<ShiftPeriod> periods = [new(1, new TimeOnly(8, 0), new TimeOnly(12, 0))];

        Create(splitPeriods: periods).Error.Should().Be(TimekeepingErrors.SplitShiftRequiresAtLeastTwoPeriods);
    }

    [Fact]
    public void Create_Fails_WhenASplitPeriodEndsBeforeItStarts()
    {
        IReadOnlyList<ShiftPeriod> periods =
        [
            new(1, new TimeOnly(12, 0), new TimeOnly(8, 0)),
            new(2, new TimeOnly(13, 0), new TimeOnly(17, 0)),
        ];

        Create(splitPeriods: periods).Error.Should().Be(TimekeepingErrors.ShiftPeriodEndNotAfterStart);
    }

    [Fact]
    public void Create_Fails_WhenAMandatoryBreakHasNoDuration()
    {
        IReadOnlyList<BreakRule> breaks = [new(BreakKind.Meal, TimeSpan.Zero, null, false, true)];

        Create(breakRules: breaks).Error.Should().Be(TimekeepingErrors.MandatoryBreakRequiresPositiveDuration);
    }

    /// <summary>
    /// The reason the anchor rule lives on the shift: one place computes the work
    /// date and every consumer reads the same answer.
    /// </summary>
    [Fact]
    public void ResolveWorkDate_AnchorsAnOvernightShiftToItsStartDate_ByDefault()
    {
        var shift = Create(isOvernight: true, anchorRule: WorkDateAnchorRule.AnchoredToShiftStart).Value;
        var startDate = TestTimekeeping.Today;

        shift.ResolveWorkDate(startDate).Should().Be(startDate);
    }

    [Fact]
    public void ResolveWorkDate_AnchorsToTheEndDate_WhenTheRuleSaysSo()
    {
        var rule = WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftEnd, null);
        var shift = Create(isOvernight: true, anchorRule: rule).Value;
        var startDate = TestTimekeeping.Today;

        shift.ResolveWorkDate(startDate).Should().Be(startDate.AddDays(1));
    }

    [Fact]
    public void ResolveWorkDate_ReturnsTheStartDate_ForANonOvernightShift()
    {
        var shift = Create().Value;

        shift.ResolveWorkDate(TestTimekeeping.Today).Should().Be(TestTimekeeping.Today);
    }

    [Fact]
    public void Publish_MakesTheShiftActive_AndRaisesTheEvent()
    {
        var shift = Create().Value;

        var result = shift.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(WorkShiftStatus.Active);
        shift.DomainEvents.OfType<WorkShiftPublished>().Should().ContainSingle();
    }

    [Fact]
    public void Publish_Fails_WhenAlreadyActive()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId);

        shift.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc).Error.Should().Be(TimekeepingErrors.VersionNotActive);
    }

    /// <summary>
    /// TK-022's signalling half: the narrower event is raised only when that one flag
    /// actually moved, so a consumer watching overtime behaviour is not woken by
    /// unrelated revisions.
    /// </summary>
    [Fact]
    public void Supersede_RaisesOvertimeEligibilityChanged_OnlyWhenTheFlagMoved()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId);

        var next = shift.Supersede(
            new WorkShiftId(Guid.NewGuid()), "Revised", TestTimekeeping.FixedTiming(), false, null, null, null,
            false, PremiumEligibilityFlags.None, TestTimekeeping.Today.AddDays(10), Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;

        next.DomainEvents.OfType<WorkShiftOvertimeEligibilityChanged>().Should().ContainSingle()
            .Which.NewValue.Should().BeFalse();
    }

    [Fact]
    public void Supersede_DoesNotRaiseOvertimeEligibilityChanged_WhenTheFlagIsUnchanged()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId);

        var next = shift.Supersede(
            new WorkShiftId(Guid.NewGuid()), "Revised", TestTimekeeping.FixedTiming(), false, null, null, null,
            true, PremiumEligibilityFlags.None, TestTimekeeping.Today.AddDays(10), Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;

        next.DomainEvents.OfType<WorkShiftOvertimeEligibilityChanged>().Should().BeEmpty();
    }

    [Fact]
    public void Supersede_Fails_WhenTheShiftIsNotActive()
    {
        var draft = Create().Value;

        draft.Supersede(
            new WorkShiftId(Guid.NewGuid()), "Revised", TestTimekeeping.FixedTiming(), false, null, null, null, true,
            PremiumEligibilityFlags.None, TestTimekeeping.Today.AddDays(10), Guid.NewGuid(), TestTimekeeping.NowUtc)
            .Error.Should().Be(TimekeepingErrors.VersionNotActive);
    }

    [Fact]
    public void Retire_IsIdempotent()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId);
        shift.Retire(Guid.NewGuid(), TestTimekeeping.Today, TestTimekeeping.NowUtc);

        var second = shift.Retire(Guid.NewGuid(), TestTimekeeping.Today, TestTimekeeping.NowUtc);

        second.IsSuccess.Should().BeTrue();
        shift.DomainEvents.OfType<WorkShiftRetired>().Should().ContainSingle();
    }
}
