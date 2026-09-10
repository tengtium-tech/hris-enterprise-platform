using FluentAssertions;
using Hris.Modules.Timekeeping.Application.Commands;
using Hris.Modules.Timekeeping.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Application;

public sealed class WorkScheduleCommandHandlerTests
{
    private readonly IWorkScheduleRepository _repository = Substitute.For<IWorkScheduleRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    private static FakeTimeProvider Clock => new(TestTimekeeping.NowUtc);

    [Fact]
    public async Task Define_AddsTheSchedule()
    {
        var handler = new DefineWorkScheduleCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new DefineWorkScheduleCommand(
                _tenantId, "Standard", null, TestTimekeeping.MondayToFriday, new TimeOnly(9, 0), new TimeOnly(18, 0),
                null, Today, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<WorkSchedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Define_Fails_WhenStandardHoursAreInverted()
    {
        var handler = new DefineWorkScheduleCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new DefineWorkScheduleCommand(
                _tenantId, "Standard", null, TestTimekeeping.MondayToFriday, new TimeOnly(18, 0), new TimeOnly(9, 0),
                null, Today, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.TimeWindowEndNotAfterStart);
    }

    [Fact]
    public async Task Define_ConvertsBreakInputs()
    {
        var handler = new DefineWorkScheduleCommandHandler(_repository, Clock);
        IReadOnlyList<BreakRuleInput> breaks =
        [
            new(BreakKind.Meal, TimeSpan.FromMinutes(60), new TimeOnly(12, 0), new TimeOnly(13, 0), false, true),
        ];

        var result = await handler.Handle(
            new DefineWorkScheduleCommand(
                _tenantId, "Standard", null, TestTimekeeping.MondayToFriday, null, null, breaks, Today, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<WorkSchedule>(s => s.BreakPeriods.Count == 1 && s.BreakPeriods[0].Window != null),
            Arg.Any<CancellationToken>());
    }

    /// <summary>CTR-ISO-002: an out-of-tenant identifier is not-found, never forbidden.</summary>
    [Fact]
    public async Task Publish_ReturnsNotFound_ForAnotherTenantsSchedule()
    {
        var schedule = TestTimekeeping.Schedule(Guid.NewGuid());
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new PublishWorkScheduleCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new PublishWorkScheduleCommand(_tenantId, schedule.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.WorkScheduleNotFound);
    }

    [Fact]
    public async Task Publish_Succeeds_WithinTheOwningTenant()
    {
        var schedule = TestTimekeeping.Schedule(_tenantId);
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new PublishWorkScheduleCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new PublishWorkScheduleCommand(_tenantId, schedule.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(WorkScheduleStatus.Active);
    }

    [Fact]
    public async Task Revise_AddsTheNewVersion()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new ReviseWorkScheduleCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new ReviseWorkScheduleCommand(
                _tenantId, schedule.Id.Value, [DayOfWeek.Monday], null, null, null, Today.AddDays(30), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<WorkSchedule>(s => s.Version == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_AndUnassign_RoundTrip()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);

        var assigned = await new AssignWorkScheduleCommandHandler(_repository, Clock).Handle(
            new AssignWorkScheduleCommand(
                _tenantId, schedule.Id.Value, OrganizationalAssignmentLevel.Department, "dept-1", Today, null,
                Guid.NewGuid()),
            CancellationToken.None);

        assigned.IsSuccess.Should().BeTrue();

        var unassigned = await new UnassignWorkScheduleCommandHandler(_repository, Clock).Handle(
            new UnassignWorkScheduleCommand(
                _tenantId, schedule.Id.Value, assigned.Value, Today.AddDays(5), Guid.NewGuid()),
            CancellationToken.None);

        unassigned.IsSuccess.Should().BeTrue();
        schedule.ScheduleAssignments[0].EffectiveTo.Should().Be(Today.AddDays(5));
    }

    [Fact]
    public async Task Retire_Succeeds()
    {
        var schedule = TestTimekeeping.ActiveSchedule(_tenantId);
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new RetireWorkScheduleCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new RetireWorkScheduleCommand(_tenantId, schedule.Id.Value, Today, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(WorkScheduleStatus.Retired);
    }
}

public sealed class WorkShiftCommandHandlerTests
{
    private readonly IWorkShiftRepository _repository = Substitute.For<IWorkShiftRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    private static FakeTimeProvider Clock => new(TestTimekeeping.NowUtc);

    private static ShiftTimingInput FixedInput() =>
        new(ShiftTimingKind.Fixed, new TimeOnly(9, 0), new TimeOnly(18, 0), null, null, null, null, null);

    private DefineWorkShiftCommand DefineCommand(
        string? code = "DAY", bool isOvernight = false, WorkDateAnchorPoint? anchor = null,
        ShiftTimingInput? timing = null) =>
        new(_tenantId, code, "Day Shift", timing ?? FixedInput(), isOvernight, anchor, null, null, null, true, false,
            false, false, Today, Guid.NewGuid());

    [Fact]
    public async Task Define_AddsTheShift()
    {
        _repository.CodeExistsInTenantAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<WorkShiftId?>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DefineWorkShiftCommandHandler(_repository, Clock);

        var result = await handler.Handle(DefineCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<WorkShift>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Define_Fails_WhenTheCodeIsAlreadyUsedInTheTenant()
    {
        _repository.CodeExistsInTenantAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<WorkShiftId?>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DefineWorkShiftCommandHandler(_repository, Clock);

        var result = await handler.Handle(DefineCommand(), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.ShiftCodeNotUniqueWithinTenant);
    }

    [Fact]
    public async Task Define_Fails_WhenAnOvernightShiftHasNoAnchorPoint()
    {
        var handler = new DefineWorkShiftCommandHandler(_repository, Clock);

        var result = await handler.Handle(DefineCommand(isOvernight: true), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.OvernightShiftRequiresAnchorRule);
    }

    [Fact]
    public async Task Define_Succeeds_ForAnOvernightShiftWithAnAnchorPoint()
    {
        var handler = new DefineWorkShiftCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            DefineCommand(isOvernight: true, anchor: WorkDateAnchorPoint.ShiftStart), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Define_Fails_WhenTimingIsMissing()
    {
        var handler = new DefineWorkShiftCommandHandler(_repository, Clock);

        var command = new DefineWorkShiftCommand(
            _tenantId, "DAY", "Day", null, false, null, null, null, null, true, false, false, false, Today,
            Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.FixedTimingRequiresWindow);
    }

    [Fact]
    public async Task Define_Fails_WhenAFlexibleTimingIsIncomplete()
    {
        var handler = new DefineWorkShiftCommandHandler(_repository, Clock);
        var incomplete = new ShiftTimingInput(ShiftTimingKind.Flexible, null, null, null, null, null, null, null);

        var result = await handler.Handle(DefineCommand(timing: incomplete), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.FlexibleTimingRequiresAllFlexibleFields);
    }

    [Fact]
    public async Task Revise_AddsTheNextVersion()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId);
        _repository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);
        var handler = new ReviseWorkShiftCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new ReviseWorkShiftCommand(
                _tenantId, shift.Id.Value, "Revised", FixedInput(), false, null, null, null, null, false, false, false,
                false, Today.AddDays(30), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<WorkShift>(s => s.Version == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_And_Retire_Succeed()
    {
        var shift = TestTimekeeping.Shift(_tenantId);
        _repository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);

        var published = await new PublishWorkShiftCommandHandler(_repository, Clock).Handle(
            new PublishWorkShiftCommand(_tenantId, shift.Id.Value, Guid.NewGuid()), CancellationToken.None);
        published.IsSuccess.Should().BeTrue();

        var retired = await new RetireWorkShiftCommandHandler(_repository, Clock).Handle(
            new RetireWorkShiftCommand(_tenantId, shift.Id.Value, Today, Guid.NewGuid()), CancellationToken.None);

        retired.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(WorkShiftStatus.Retired);
    }

    [Fact]
    public async Task Publish_ReturnsNotFound_ForAnotherTenantsShift()
    {
        var shift = TestTimekeeping.Shift(Guid.NewGuid());
        _repository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);
        var handler = new PublishWorkShiftCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new PublishWorkShiftCommand(_tenantId, shift.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.WorkShiftNotFound);
    }
}
