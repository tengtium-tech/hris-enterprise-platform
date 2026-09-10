using FluentAssertions;
using Hris.Modules.Timekeeping.Domain;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

public sealed class WorkingDayPatternTests
{
    [Fact]
    public void Create_Fails_WithNoWorkingDays()
    {
        WorkingDayPattern.Create(null).Error.Should().Be(TimekeepingErrors.WorkingDayPatternRequiresAWorkingDay);
        WorkingDayPattern.Create([]).Error.Should().Be(TimekeepingErrors.WorkingDayPatternRequiresAWorkingDay);
    }

    /// <summary>
    /// The partition property: rest days are derived, so no caller can supply a pair
    /// that contradicts itself.
    /// </summary>
    [Fact]
    public void RestDays_AreTheExactComplementOfWorkingDays()
    {
        var pattern = WorkingDayPattern.Create(TestTimekeeping.MondayToFriday).Value;

        pattern.WorkingDays.Should().HaveCount(5);
        pattern.RestDays.Should().BeEquivalentTo([DayOfWeek.Saturday, DayOfWeek.Sunday]);
        pattern.WorkingDays.Intersect(pattern.RestDays).Should().BeEmpty();
        pattern.WorkingDays.Concat(pattern.RestDays).Should().HaveCount(7);
    }

    [Fact]
    public void Create_DeduplicatesRepeatedDays()
    {
        var pattern = WorkingDayPattern.Create([DayOfWeek.Monday, DayOfWeek.Monday]).Value;

        pattern.WorkingDays.Should().ContainSingle();
        pattern.RestDays.Should().HaveCount(6);
    }

    [Fact]
    public void IsWorkingDay_AnswersForBothADayAndADate()
    {
        var pattern = WorkingDayPattern.Create(TestTimekeeping.MondayToFriday).Value;
        var saturday = new DateOnly(2026, 9, 12);

        saturday.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        pattern.IsWorkingDay(DayOfWeek.Monday).Should().BeTrue();
        pattern.IsWorkingDay(saturday).Should().BeFalse();
    }

    [Fact]
    public void EqualityIsByValue()
    {
        WorkingDayPattern.Create(TestTimekeeping.MondayToFriday).Value
            .Should().Be(WorkingDayPattern.Create(TestTimekeeping.MondayToFriday).Value);
    }
}

public sealed class TimeWindowTests
{
    [Fact]
    public void Create_Fails_WhenEndIsNotAfterStart()
    {
        TimeWindow.Create(new TimeOnly(17, 0), new TimeOnly(9, 0)).Error
            .Should().Be(TimekeepingErrors.TimeWindowEndNotAfterStart);

        TimeWindow.Create(new TimeOnly(9, 0), new TimeOnly(9, 0)).Error
            .Should().Be(TimekeepingErrors.TimeWindowEndNotAfterStart);
    }

    [Fact]
    public void Duration_AndContains_BehaveAsExpected()
    {
        var window = TestTimekeeping.Window(9, 18);

        window.Duration.Should().Be(TimeSpan.FromHours(9));
        window.Contains(new TimeOnly(12, 0)).Should().BeTrue();
        window.Contains(new TimeOnly(20, 0)).Should().BeFalse();
    }

    [Fact]
    public void EqualityIsByValue_AndToStringIsReadable()
    {
        TestTimekeeping.Window(9, 18).Should().Be(TestTimekeeping.Window(9, 18));
        TestTimekeeping.Window(9, 18).ToString().Should().Be("09:00-18:00");
    }
}

public sealed class ShiftTimingTests
{
    [Fact]
    public void Fixed_Fails_WithoutAWindow()
    {
        ShiftTiming.Fixed(null).Error.Should().Be(TimekeepingErrors.FixedTimingRequiresWindow);
    }

    [Fact]
    public void Fixed_CarriesNoFlexibleFields()
    {
        var timing = TestTimekeeping.FixedTiming();

        timing.Kind.Should().Be(ShiftTimingKind.Fixed);
        timing.FixedWindow.Should().NotBeNull();
        timing.EarliestStart.Should().BeNull();
        timing.CoreHours.Should().BeNull();
        timing.RequiredHours.Should().BeNull();
    }

    [Fact]
    public void Flexible_Fails_WhenAnyFlexibleFieldIsMissing()
    {
        ShiftTiming.Flexible(null, new TimeOnly(10, 0), TestTimekeeping.Window(10, 15), TimeSpan.FromHours(8)).Error
            .Should().Be(TimekeepingErrors.FlexibleTimingRequiresAllFlexibleFields);

        ShiftTiming.Flexible(new TimeOnly(7, 0), new TimeOnly(10, 0), TestTimekeeping.Window(10, 15), null).Error
            .Should().Be(TimekeepingErrors.FlexibleTimingRequiresAllFlexibleFields);
    }

    [Fact]
    public void Flexible_Fails_WhenRequiredHoursIsNotPositive()
    {
        ShiftTiming.Flexible(new TimeOnly(7, 0), new TimeOnly(10, 0), TestTimekeeping.Window(10, 15), TimeSpan.Zero)
            .Error.Should().Be(TimekeepingErrors.RequiredHoursMustBePositive);
    }

    [Fact]
    public void Flexible_Fails_WhenEarliestIsNotBeforeLatest()
    {
        ShiftTiming.Flexible(
            new TimeOnly(11, 0), new TimeOnly(10, 0), TestTimekeeping.Window(11, 15), TimeSpan.FromHours(8)).Error
            .Should().Be(TimekeepingErrors.FlexibleTimingEarliestNotBeforeLatest);
    }

    /// <summary>
    /// Core hours must be reachable by someone starting at the earliest permitted time
    /// and by someone starting at the latest; otherwise no valid start can meet them.
    /// </summary>
    [Fact]
    public void Flexible_Fails_WhenCoreHoursFallOutsideTheReachableRange()
    {
        ShiftTiming.Flexible(
            new TimeOnly(9, 0), new TimeOnly(10, 0), TestTimekeeping.Window(6, 8), TimeSpan.FromHours(8)).Error
            .Should().Be(TimekeepingErrors.FlexibleTimingCoreHoursOutsideRange);
    }

    [Fact]
    public void Flexible_Succeeds_ForAReachableCoreWindow()
    {
        var timing = ShiftTiming.Flexible(
            new TimeOnly(7, 0), new TimeOnly(10, 0), TestTimekeeping.Window(10, 15), TimeSpan.FromHours(8));

        timing.IsSuccess.Should().BeTrue();
        timing.Value.Kind.Should().Be(ShiftTimingKind.Flexible);
        timing.Value.FixedWindow.Should().BeNull();
    }
}

public sealed class WorkDateAnchorRuleTests
{
    [Fact]
    public void AnchoredToShiftStart_IsThePlatformDefault_AndCarriesADescription()
    {
        var rule = WorkDateAnchorRule.AnchoredToShiftStart;

        rule.AnchorPoint.Should().Be(WorkDateAnchorPoint.ShiftStart);
        rule.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_UsesTheSuppliedDescription_WhenGivenOne()
    {
        WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftEnd, "Ends the next morning").Description
            .Should().Be("Ends the next morning");
    }

    [Fact]
    public void ResolveWorkDate_DependsOnTheAnchorPoint()
    {
        var start = TestTimekeeping.Today;

        WorkDateAnchorRule.AnchoredToShiftStart.ResolveWorkDate(start).Should().Be(start);
        WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftEnd, null).ResolveWorkDate(start)
            .Should().Be(start.AddDays(1));
    }
}

public sealed class ShiftCodeAndPremiumTests
{
    [Fact]
    public void ShiftCode_Fails_WhenMissingOrTooLong()
    {
        ShiftCode.Create(" ").Error.Should().Be(TimekeepingErrors.ShiftCodeRequired);
        ShiftCode.Create(new string('x', ShiftCode.MaximumLength + 1)).Error
            .Should().Be(TimekeepingErrors.ShiftCodeRequired);
    }

    [Fact]
    public void ShiftCode_TrimsAndComparesByValue()
    {
        ShiftCode.Create("  DAY  ").Value.Value.Should().Be("DAY");
        ShiftCode.Create("DAY").Value.Should().Be(ShiftCode.Create("DAY").Value);
        ShiftCode.Create("DAY").Value.ToString().Should().Be("DAY");
    }

    /// <summary>
    /// Flags only. No rate belongs here — the peso value is payroll's, computed
    /// against the flag this module states.
    /// </summary>
    [Fact]
    public void PremiumEligibilityFlags_CarryNoRate()
    {
        var properties = typeof(PremiumEligibilityFlags).GetProperties().Select(p => p.Name).ToList();

        properties.Should().BeEquivalentTo(
            ["NightDifferentialEligible", "HazardEligible", "HolidayPremiumEligible", "None"]);
    }

    [Fact]
    public void PremiumEligibilityFlags_CompareByValue()
    {
        PremiumEligibilityFlags.Create(true, false, true)
            .Should().Be(PremiumEligibilityFlags.Create(true, false, true));

        PremiumEligibilityFlags.None.NightDifferentialEligible.Should().BeFalse();
    }
}

public sealed class ShiftPeriodTests
{
    [Fact]
    public void OverlapsWith_DetectsOverlapAndAdjacency()
    {
        var first = new ShiftPeriod(1, new TimeOnly(8, 0), new TimeOnly(12, 0));
        var overlapping = new ShiftPeriod(2, new TimeOnly(11, 0), new TimeOnly(15, 0));
        var adjacent = new ShiftPeriod(2, new TimeOnly(12, 0), new TimeOnly(15, 0));

        first.OverlapsWith(overlapping).Should().BeTrue();
        first.OverlapsWith(adjacent).Should().BeFalse("touching at the boundary is the break, not an overlap");
    }

    [Fact]
    public void OverlapsWith_Throws_OnNull()
    {
        var period = new ShiftPeriod(1, new TimeOnly(8, 0), new TimeOnly(12, 0));

        var act = () => period.OverlapsWith(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
