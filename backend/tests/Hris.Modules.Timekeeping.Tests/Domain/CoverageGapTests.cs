using FluentAssertions;
using Hris.Modules.Timekeeping.Application.Commands;
using Hris.Modules.Timekeeping.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

/// <summary>
/// Paths the main suites left uncovered. Each is real behaviour or real value
/// semantics — no test here exists only to move a coverage number, which
/// unit-and-integration-testing.md §6 warns adds maintenance cost without adding
/// assurance.
/// </summary>
public sealed class CoverageGapTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;
    private static FakeTimeProvider Clock => new(TestTimekeeping.NowUtc);

    /// <summary>
    /// TK-002 on the shift, the counterpart of the schedule assertion in
    /// <see cref="TemporalCorrectnessTests"/>: a superseded shift version stays
    /// effective for the dates it governed and the new one does not reach back.
    /// </summary>
    [Fact]
    public void WorkShift_IsEffectiveOn_BoundsEachVersionToItsOwnDateRange()
    {
        var v1 = TestTimekeeping.ActiveShift(_tenantId, effectiveFrom: Today);
        var changeover = Today.AddDays(30);

        var v2 = v1.Supersede(
            new WorkShiftId(Guid.NewGuid()), "Revised", TestTimekeeping.FixedTiming(6, 14), false, null, null, null,
            true, PremiumEligibilityFlags.None, changeover, Guid.NewGuid(), TestTimekeeping.NowUtc).Value;

        v1.IsEffectiveOn(Today).Should().BeTrue();
        v1.IsEffectiveOn(changeover.AddDays(-1)).Should().BeTrue();
        v1.IsEffectiveOn(changeover).Should().BeFalse();

        v2.IsEffectiveOn(changeover.AddDays(-1)).Should().BeFalse();
        v2.IsEffectiveOn(changeover).Should().BeTrue();
    }

    [Fact]
    public void WorkShift_IsEffectiveOn_IsOpenEnded_ForAnUnsupersededVersion()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId, effectiveFrom: Today);

        shift.IsEffectiveOn(Today.AddYears(5)).Should().BeTrue();
        shift.IsEffectiveOn(Today.AddDays(-1)).Should().BeFalse();
    }

    /// <summary>Value semantics on the anchor rule: two rules stating the same thing are the same rule.</summary>
    [Fact]
    public void WorkDateAnchorRule_ComparesByValue()
    {
        WorkDateAnchorRule.AnchoredToShiftStart.Should().Be(WorkDateAnchorRule.AnchoredToShiftStart);

        WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftStart, "A")
            .Should().NotBe(WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftStart, "B"));

        WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftStart, null)
            .Should().NotBe(WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftEnd, null));
    }

    /// <summary>
    /// Consent state participates in equality, so a proposal that has gained a consent
    /// is not equal to the one that had not — which is what makes the pending swap
    /// safe to hold as a value.
    /// </summary>
    [Fact]
    public void PendingShiftSwap_ComparesByValue_IncludingConsentState()
    {
        var counterpart = new ShiftAssignmentId(Guid.NewGuid());

        var first = PendingShiftSwap.Create(counterpart, Today, Guid.Empty, "emp-1", "emp-2").Value;
        var second = PendingShiftSwap.Create(counterpart, Today, Guid.Empty, "emp-1", "emp-2").Value;

        first.Should().Be(second);

        second.PrimaryConsented = true;
        first.Should().NotBe(second);
    }

    [Fact]
    public void PendingShiftSwap_Create_Fails_WhenBothSidesNameTheSameEmployee()
    {
        var result = PendingShiftSwap.Create(
            new ShiftAssignmentId(Guid.NewGuid()), Today, Guid.Empty, "emp-1", "emp-1");

        result.Error.Should().Be(TimekeepingErrors.SwapRequiresTwoDistinctAssignments);
    }

    [Fact]
    public void PendingShiftSwap_Create_Fails_WhenAnEmployeeIdIsMissing()
    {
        PendingShiftSwap.Create(new ShiftAssignmentId(Guid.NewGuid()), Today, Guid.Empty, " ", "emp-2").Error
            .Should().Be(TimekeepingErrors.AssignmentTargetRequired);
    }

    /// <summary>Provenance is recorded on every aggregate, and read back by audit.</summary>
    [Fact]
    public void EveryAggregate_RecordsWhoCreatedItAndWhen()
    {
        var actor = Guid.NewGuid();

        var schedule = WorkSchedule.Create(
            new WorkScheduleId(Guid.NewGuid()), _tenantId, "S", null, TestTimekeeping.MondayToFriday, null, null,
            Today, actor, TestTimekeeping.NowUtc).Value;

        var shift = WorkShift.Create(
            new WorkShiftId(Guid.NewGuid()), _tenantId, "C", "N", TestTimekeeping.FixedTiming(), false, null, null,
            null, true, PremiumEligibilityFlags.None, Today, false, actor, TestTimekeeping.NowUtc).Value;

        var calendar = HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), null, "PH", HolidayCalendarLevel.Country, "PH", "PH", null, Today,
            actor, TestTimekeeping.NowUtc).Value;

        schedule.CreatedBy.Should().Be(actor);
        schedule.CreatedOn.Should().Be(TestTimekeeping.NowUtc);
        shift.CreatedBy.Should().Be(actor);
        shift.CreatedOn.Should().Be(TestTimekeeping.NowUtc);
        calendar.CreatedBy.Should().Be(actor);
        calendar.CreatedOn.Should().Be(TestTimekeeping.NowUtc);
    }

    /// <summary>
    /// TK-036: a rotation-generated assignment records which cycle produced it, so it
    /// stays individually auditable rather than re-derived from the rule at read time.
    /// </summary>
    [Fact]
    public void RotationCycleReference_IsRetainedOnTheAssignment()
    {
        var cycleId = Guid.NewGuid();

        var assignment = ShiftAssignment.Create(
            new ShiftAssignmentId(Guid.NewGuid()), _tenantId, AssignmentTargetType.Employee, "emp-1",
            OrganizationalAssignmentLevel.IndividualEmployee, new WorkShiftId(Guid.NewGuid()), Today, null, false,
            new RotationCycleId(cycleId), false, null, TestTimekeeping.NowUtc).Value;

        assignment.RotationCycleReference!.Value.Value.Should().Be(cycleId);
    }

    /// <summary>Exercises the collection-projection paths in both shift handlers.</summary>
    [Fact]
    public async Task DefineWorkShift_ProjectsSplitPeriodsAndBreakRules()
    {
        var repository = Substitute.For<IWorkShiftRepository>();
        var handler = new DefineWorkShiftCommandHandler(repository, Clock);

        IReadOnlyList<ShiftPeriodInput> periods =
        [
            new(1, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            new(2, new TimeOnly(13, 0), new TimeOnly(17, 0)),
        ];
        IReadOnlyList<BreakRuleInput> breaks =
        [
            new(BreakKind.Meal, TimeSpan.FromMinutes(60), new TimeOnly(12, 0), new TimeOnly(13, 0), false, true),
        ];

        var result = await handler.Handle(
            new DefineWorkShiftCommand(
                _tenantId, "SPLIT", "Split", new ShiftTimingInput(
                    ShiftTimingKind.Fixed, new TimeOnly(8, 0), new TimeOnly(17, 0), null, null, null, null, null),
                false, null, null, periods, breaks, true, false, false, false, Today, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(
            Arg.Is<WorkShift>(s => s.SplitPeriods.Count == 2 && s.BreakRules.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviseWorkShift_ProjectsSplitPeriodsBreakRulesAndAnchorDescription()
    {
        var repository = Substitute.For<IWorkShiftRepository>();
        var shift = TestTimekeeping.ActiveShift(_tenantId);
        repository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);
        var handler = new ReviseWorkShiftCommandHandler(repository, Clock);

        IReadOnlyList<ShiftPeriodInput> periods =
        [
            new(1, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            new(2, new TimeOnly(13, 0), new TimeOnly(17, 0)),
        ];
        IReadOnlyList<BreakRuleInput> breaks =
        [
            new(BreakKind.Rest, TimeSpan.FromMinutes(15), null, null, true, false),
        ];

        var result = await handler.Handle(
            new ReviseWorkShiftCommand(
                _tenantId, shift.Id.Value, "Night", new ShiftTimingInput(
                    ShiftTimingKind.Fixed, new TimeOnly(22, 0), new TimeOnly(23, 0), null, null, null, null, null),
                true, WorkDateAnchorPoint.ShiftEnd, "Ends next morning", periods, breaks, true, true, false, false,
                Today.AddDays(30), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(
            Arg.Is<WorkShift>(s =>
                s.SplitPeriods.Count == 2
                && s.BreakRules.Count == 1
                && s.AnchorRule!.Description == "Ends next morning"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Exercises the optional standard-hours path on a schedule revision.</summary>
    [Fact]
    public async Task ReviseWorkSchedule_CarriesStandardHoursThrough()
    {
        var repository = Substitute.For<IWorkScheduleRepository>();
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new ReviseWorkScheduleCommandHandler(repository, Clock);

        var result = await handler.Handle(
            new ReviseWorkScheduleCommand(
                _tenantId, schedule.Id.Value, TestTimekeeping.MondayToFriday, new TimeOnly(8, 0), new TimeOnly(17, 0),
                null, Today.AddDays(30), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(
            Arg.Is<WorkSchedule>(s => s.StandardHours!.Start == new TimeOnly(8, 0)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviseWorkSchedule_Fails_OnAnInvertedStandardHoursWindow()
    {
        var repository = Substitute.For<IWorkScheduleRepository>();
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new ReviseWorkScheduleCommandHandler(repository, Clock);

        var result = await handler.Handle(
            new ReviseWorkScheduleCommand(
                _tenantId, schedule.Id.Value, TestTimekeeping.MondayToFriday, new TimeOnly(17, 0), new TimeOnly(8, 0),
                null, Today.AddDays(30), Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.TimeWindowEndNotAfterStart);
    }
}
