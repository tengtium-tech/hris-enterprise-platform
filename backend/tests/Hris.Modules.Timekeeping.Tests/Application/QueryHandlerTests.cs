using FluentAssertions;
using Hris.Modules.Timekeeping.Application.Queries;
using Hris.Modules.Timekeeping.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Application;

public sealed class TimekeepingQueryHandlerTests
{
    private readonly IWorkScheduleRepository _schedules = Substitute.For<IWorkScheduleRepository>();
    private readonly IWorkShiftRepository _shifts = Substitute.For<IWorkShiftRepository>();
    private readonly IShiftAssignmentRepository _assignments = Substitute.For<IShiftAssignmentRepository>();
    private readonly IHolidayCalendarRepository _calendars = Substitute.For<IHolidayCalendarRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    [Fact]
    public async Task GetWorkSchedule_ReturnsTheDto()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        _schedules.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new GetWorkScheduleQueryHandler(_schedules);

        var result = await handler.Handle(
            new GetWorkScheduleQuery(schedule.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(schedule.Name);
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetWorkSchedule_ReturnsNotFound_ForAnotherTenant()
    {
        var schedule = TestTimekeeping.ActiveSchedule(Guid.NewGuid());
        _schedules.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new GetWorkScheduleQueryHandler(_schedules);

        var result = await handler.Handle(
            new GetWorkScheduleQuery(schedule.Id.Value, _tenantId), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.WorkScheduleNotFound);
    }

    [Fact]
    public async Task ListWorkSchedules_FiltersByStatusCaseInsensitively()
    {
        _schedules.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns([TestTimekeeping.ActiveSchedule(_tenantId), TestTimekeeping.Schedule(_tenantId)]);
        var handler = new ListWorkSchedulesQueryHandler(_schedules);

        var all = await handler.Handle(new ListWorkSchedulesQuery(_tenantId, null), CancellationToken.None);
        var active = await handler.Handle(new ListWorkSchedulesQuery(_tenantId, "active"), CancellationToken.None);

        all.Value.Should().HaveCount(2);
        active.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetWorkShift_And_ListWorkShifts_Work()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId);
        _shifts.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);
        _shifts.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns([shift]);

        var single = await new GetWorkShiftQueryHandler(_shifts).Handle(
            new GetWorkShiftQuery(shift.Id.Value, _tenantId), CancellationToken.None);
        var list = await new ListWorkShiftsQueryHandler(_shifts).Handle(
            new ListWorkShiftsQuery(_tenantId, "Active"), CancellationToken.None);

        single.Value.Code.Should().Be("DAY");
        list.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetWorkShift_ReturnsNotFound_WhenMissing()
    {
        _shifts.GetByIdAsync(Arg.Any<WorkShiftId>(), Arg.Any<CancellationToken>()).Returns((WorkShift?)null);

        var result = await new GetWorkShiftQueryHandler(_shifts).Handle(
            new GetWorkShiftQuery(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.WorkShiftNotFound);
    }

    [Fact]
    public async Task ListShiftAssignments_ReadsByTargetWhenGiven_AndByTenantOtherwise()
    {
        var assignment = TestTimekeeping.Assignment(_tenantId, "emp-1");
        _assignments.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns([assignment]);
        _assignments.ListByTargetAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>()).Returns([assignment]);
        var handler = new ListShiftAssignmentsQueryHandler(_assignments);

        var byTenant = await handler.Handle(new ListShiftAssignmentsQuery(_tenantId, null), CancellationToken.None);
        var byTarget = await handler.Handle(new ListShiftAssignmentsQuery(_tenantId, "emp-1"), CancellationToken.None);

        byTenant.Value.Should().ContainSingle();
        byTarget.Value.Should().ContainSingle();
        await _assignments.Received(1).ListByTargetAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHolidayCalendar_ReturnsTheDtoWithOrderedHolidays()
    {
        var country = TestTimekeeping.CountryCalendar();
        country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today.AddDays(10), "Later", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc);
        country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Earlier", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc);
        _calendars.GetByIdAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);

        var result = await new GetHolidayCalendarQueryHandler(_calendars).Handle(
            new GetHolidayCalendarQuery(country.Id.Value, _tenantId), CancellationToken.None);

        result.Value.Holidays.Select(h => h.Name).Should().ContainInOrder("Earlier", "Later");
    }
}

/// <summary>
/// The queries this module exists to answer. These are what <c>attendance</c> will
/// call, so they get the same weight as the domain rules they surface.
/// </summary>
public sealed class ResolutionQueryHandlerTests
{
    private readonly IShiftAssignmentRepository _assignments = Substitute.For<IShiftAssignmentRepository>();
    private readonly IHolidayCalendarRepository _calendars = Substitute.For<IHolidayCalendarRepository>();
    private readonly IWorkScheduleRepository _schedules = Substitute.For<IWorkScheduleRepository>();
    private readonly IWorkShiftRepository _shifts = Substitute.For<IWorkShiftRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    [Fact]
    public async Task ResolveShift_AggregatesCandidatesAcrossEveryTargetAndPicksTheMostSpecific()
    {
        var individual = TestTimekeeping.Assignment(_tenantId, "emp-1");
        var department = TestTimekeeping.Assignment(
            _tenantId, "dept-1", OrganizationalAssignmentLevel.Department);

        _assignments.ListByTargetAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>()).Returns([individual]);
        _assignments.ListByTargetAsync(_tenantId, "dept-1", Arg.Any<CancellationToken>()).Returns([department]);

        var handler = new ResolveShiftForEmployeeOnDateQueryHandler(_assignments);

        var result = await handler.Handle(
            new ResolveShiftForEmployeeOnDateQuery(_tenantId, ["emp-1", "dept-1"], Today), CancellationToken.None);

        result.Value.Assignment!.Id.Should().Be(individual.Id.Value);
        result.Value.WorkShiftId.Should().Be(individual.WorkShiftId.Value);
        result.Value.IsUnresolved.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveShift_ReportsUnresolved_WhenNothingApplies()
    {
        _assignments.ListByTargetAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new ResolveShiftForEmployeeOnDateQueryHandler(_assignments);

        var result = await handler.Handle(
            new ResolveShiftForEmployeeOnDateQuery(_tenantId, ["emp-1"], Today), CancellationToken.None);

        result.Value.IsUnresolved.Should().BeTrue();
        result.Value.Assignment.Should().BeNull();
    }

    [Fact]
    public async Task ResolveShift_ReportsAmbiguous_OnASameLevelTie()
    {
        var first = TestTimekeeping.Assignment(_tenantId, "dept-1", OrganizationalAssignmentLevel.Department);
        var second = TestTimekeeping.Assignment(_tenantId, "dept-2", OrganizationalAssignmentLevel.Department);
        _assignments.ListByTargetAsync(_tenantId, "dept-1", Arg.Any<CancellationToken>()).Returns([first]);
        _assignments.ListByTargetAsync(_tenantId, "dept-2", Arg.Any<CancellationToken>()).Returns([second]);
        var handler = new ResolveShiftForEmployeeOnDateQueryHandler(_assignments);

        var result = await handler.Handle(
            new ResolveShiftForEmployeeOnDateQuery(_tenantId, ["dept-1", "dept-2"], Today), CancellationToken.None);

        result.Value.IsAmbiguous.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveShift_SkipsBlankAndDuplicateTargets()
    {
        var assignment = TestTimekeeping.Assignment(_tenantId, "emp-1");
        _assignments.ListByTargetAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>()).Returns([assignment]);
        var handler = new ResolveShiftForEmployeeOnDateQueryHandler(_assignments);

        var result = await handler.Handle(
            new ResolveShiftForEmployeeOnDateQuery(_tenantId, ["emp-1", "emp-1", "  "], Today), CancellationToken.None);

        result.Value.Assignment.Should().NotBeNull();
        await _assignments.Received(1).ListByTargetAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveHoliday_ReturnsTheMostSpecificLayer()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        var company = TestTimekeeping.CompanyCalendar(Guid.NewGuid(), country.Id);
        _calendars.GetByIdAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);
        _calendars.ListLayerChainAsync(country.Id, Arg.Any<CancellationToken>()).Returns([country]);

        var handler = new ResolveHolidayForScopeOnDateQueryHandler(_calendars);

        var result = await handler.Handle(
            new ResolveHolidayForScopeOnDateQuery(company.TenantId!.Value, country.Id.Value, Today),
            CancellationToken.None);

        result.Value.IsHoliday.Should().BeTrue();
        result.Value.SourceLevel.Should().Be("Country");
    }

    [Fact]
    public async Task ResolveHoliday_ReportsNotAHoliday_WhenNoLayerDeclaresTheDate()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(30));
        _calendars.GetByIdAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);
        _calendars.ListLayerChainAsync(country.Id, Arg.Any<CancellationToken>()).Returns([country]);
        var handler = new ResolveHolidayForScopeOnDateQueryHandler(_calendars);

        var result = await handler.Handle(
            new ResolveHolidayForScopeOnDateQuery(_tenantId, country.Id.Value, Today), CancellationToken.None);

        result.Value.IsHoliday.Should().BeFalse();
        result.Value.Holiday.Should().BeNull();
    }

    [Fact]
    public async Task ResolveHoliday_ReturnsNotFound_WhenTheLeafCalendarIsMissing()
    {
        _calendars.GetByIdAsync(Arg.Any<HolidayCalendarId>(), Arg.Any<CancellationToken>())
            .Returns((HolidayCalendar?)null);
        var handler = new ResolveHolidayForScopeOnDateQueryHandler(_calendars);

        var result = await handler.Handle(
            new ResolveHolidayForScopeOnDateQuery(_tenantId, Guid.NewGuid(), Today), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.HolidayCalendarNotFound);
    }

    /// <summary>
    /// CTR-DAT-005 for this module: revise a schedule effective next month, ask about
    /// a date last month, get the prior version back unchanged.
    /// </summary>
    [Fact]
    public async Task ResolveSchedule_ReturnsTheVersionInForceOnTheDateAsked_NotTheCurrentOne()
    {
        var v1 = TestTimekeeping.ActiveSchedule(_tenantId, Today.AddDays(-60));
        v1.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1",
            Today.AddDays(-60), null, Guid.NewGuid(), TestTimekeeping.NowUtc);

        var v2 = v1.Supersede(
            new WorkScheduleId(Guid.NewGuid()), [DayOfWeek.Sunday], null, null, Today, Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;
        v2.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);
        v2.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), OrganizationalAssignmentLevel.Department, "dept-1", Today, null,
            Guid.NewGuid(), TestTimekeeping.NowUtc);

        _schedules.ListByLineageAsync(v1.LineageId, Arg.Any<CancellationToken>()).Returns([v1, v2]);
        var handler = new ResolveScheduleForOrganizationalUnitOnDateQueryHandler(_schedules);

        var pastDate = Today.AddDays(-30);

        var result = await handler.Handle(
            new ResolveScheduleForOrganizationalUnitOnDateQuery(
                _tenantId, v1.LineageId, OrganizationalAssignmentLevel.Department, "dept-1", pastDate),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(1, "the version in force on that date, not the version current now");
        result.Value.WorkingDays.Should().Contain("Monday");
    }

    [Fact]
    public async Task ResolveSchedule_ReturnsTheCurrentVersion_ForAPresentDate()
    {
        var v1 = TestTimekeeping.ActiveSchedule(_tenantId, Today.AddDays(-60));
        var v2 = v1.Supersede(
            new WorkScheduleId(Guid.NewGuid()), [DayOfWeek.Sunday], null, null, Today, Guid.NewGuid(),
            TestTimekeeping.NowUtc).Value;
        v2.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);

        _schedules.ListByLineageAsync(v1.LineageId, Arg.Any<CancellationToken>()).Returns([v1, v2]);
        var handler = new ResolveScheduleForOrganizationalUnitOnDateQueryHandler(_schedules);

        var result = await handler.Handle(
            new ResolveScheduleForOrganizationalUnitOnDateQuery(
                _tenantId, v1.LineageId, OrganizationalAssignmentLevel.Department, null, Today),
            CancellationToken.None);

        result.Value.Version.Should().Be(2);
    }

    [Fact]
    public async Task ResolveSchedule_ReturnsNotFound_WhenNoVersionGovernsTheDate()
    {
        _schedules.ListByLineageAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new ResolveScheduleForOrganizationalUnitOnDateQueryHandler(_schedules);

        var result = await handler.Handle(
            new ResolveScheduleForOrganizationalUnitOnDateQuery(
                _tenantId, Guid.NewGuid(), OrganizationalAssignmentLevel.Department, "dept-1", Today),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.WorkScheduleNotFound);
    }

    [Fact]
    public async Task ResolveWorkDate_AnswersFromTheShiftsOwnAnchorRule()
    {
        var overnight = WorkShift.Create(
            new WorkShiftId(Guid.NewGuid()), _tenantId, "NIGHT", "Night Shift", TestTimekeeping.FixedTiming(22, 23),
            true, WorkDateAnchorRule.Create(WorkDateAnchorPoint.ShiftEnd, null), null, null, true,
            PremiumEligibilityFlags.None, Today, false, Guid.NewGuid(), TestTimekeeping.NowUtc).Value;

        _shifts.GetByIdAsync(overnight.Id, Arg.Any<CancellationToken>()).Returns(overnight);
        var handler = new ResolveWorkDateForShiftInstantQueryHandler(_shifts);

        var result = await handler.Handle(
            new ResolveWorkDateForShiftInstantQuery(_tenantId, overnight.Id.Value, Today), CancellationToken.None);

        result.Value.Should().Be(Today.AddDays(1));
    }

    [Fact]
    public async Task ResolveWorkDate_ReturnsNotFound_ForAnUnknownShift()
    {
        _shifts.GetByIdAsync(Arg.Any<WorkShiftId>(), Arg.Any<CancellationToken>()).Returns((WorkShift?)null);
        var handler = new ResolveWorkDateForShiftInstantQueryHandler(_shifts);

        var result = await handler.Handle(
            new ResolveWorkDateForShiftInstantQuery(_tenantId, Guid.NewGuid(), Today), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.WorkShiftNotFound);
    }
}
