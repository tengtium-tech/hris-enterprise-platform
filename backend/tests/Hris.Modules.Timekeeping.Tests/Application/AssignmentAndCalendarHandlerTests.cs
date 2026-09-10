using FluentAssertions;
using Hris.Modules.Timekeeping.Application.Commands;
using Hris.Modules.Timekeeping.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Application;

public sealed class ShiftAssignmentCommandHandlerTests
{
    private readonly IShiftAssignmentRepository _repository = Substitute.For<IShiftAssignmentRepository>();
    private readonly IWorkShiftRepository _shiftRepository = Substitute.For<IWorkShiftRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;
    private static FakeTimeProvider Clock => new(TestTimekeeping.NowUtc);

    private WorkShift ArrangeShift()
    {
        var shift = TestTimekeeping.ActiveShift(_tenantId);
        _shiftRepository.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);
        return shift;
    }

    [Fact]
    public async Task Create_AddsTheAssignment()
    {
        var shift = ArrangeShift();
        _repository.ListIndividualByEmployeeAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateShiftAssignmentCommandHandler(_repository, _shiftRepository, Clock);

        var result = await handler.Handle(
            new CreateShiftAssignmentCommand(
                _tenantId, AssignmentTargetType.Employee, "emp-1", OrganizationalAssignmentLevel.IndividualEmployee,
                shift.Id.Value, Today, null, false, null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<ShiftAssignment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Every assignment references a shift that exists — checkable only here.</summary>
    [Fact]
    public async Task Create_Fails_WhenTheReferencedShiftDoesNotExist()
    {
        _shiftRepository.GetByIdAsync(Arg.Any<WorkShiftId>(), Arg.Any<CancellationToken>()).Returns((WorkShift?)null);
        var handler = new CreateShiftAssignmentCommandHandler(_repository, _shiftRepository, Clock);

        var result = await handler.Handle(
            new CreateShiftAssignmentCommand(
                _tenantId, AssignmentTargetType.Employee, "emp-1", OrganizationalAssignmentLevel.IndividualEmployee,
                Guid.NewGuid(), Today, null, false, null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.WorkShiftNotFound);
    }

    /// <summary>TK-031, computed by the handler across the employee's other assignments.</summary>
    [Fact]
    public async Task Create_Fails_WhenAnIndividualAssignmentOverlapsAnExistingOne()
    {
        var shift = ArrangeShift();
        var existing = TestTimekeeping.Assignment(_tenantId, "emp-1", from: Today, to: Today.AddDays(30));
        existing.Activate();
        _repository.ListIndividualByEmployeeAsync(_tenantId, "emp-1", Arg.Any<CancellationToken>()).Returns([existing]);
        var handler = new CreateShiftAssignmentCommandHandler(_repository, _shiftRepository, Clock);

        var result = await handler.Handle(
            new CreateShiftAssignmentCommand(
                _tenantId, AssignmentTargetType.Employee, "emp-1", OrganizationalAssignmentLevel.IndividualEmployee,
                shift.Id.Value, Today.AddDays(10), Today.AddDays(40), false, null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.OverlappingIndividualAssignment);
    }

    [Fact]
    public async Task Create_DoesNotConsultOverlap_ForANonIndividualLevel()
    {
        var shift = ArrangeShift();
        var handler = new CreateShiftAssignmentCommandHandler(_repository, _shiftRepository, Clock);

        var result = await handler.Handle(
            new CreateShiftAssignmentCommand(
                _tenantId, AssignmentTargetType.OrganizationalUnit, "dept-1", OrganizationalAssignmentLevel.Department,
                shift.Id.Value, Today, null, false, null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().ListIndividualByEmployeeAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_Succeeds()
    {
        var assignment = TestTimekeeping.Assignment(_tenantId, "emp-1");
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        var handler = new CancelShiftAssignmentCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new CancelShiftAssignmentCommand(_tenantId, assignment.Id.Value, "Restructure", Today, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.Status.Should().Be(ShiftAssignmentStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_ForAnotherTenantsAssignment()
    {
        var assignment = TestTimekeeping.Assignment(Guid.NewGuid(), "emp-1");
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        var handler = new CancelShiftAssignmentCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new CancelShiftAssignmentCommand(_tenantId, assignment.Id.Value, "Reason", Today, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.ShiftAssignmentNotFound);
    }

    /// <summary>
    /// The whole four-step flow end to end, ending in exactly one swap event carrying
    /// both sides.
    /// </summary>
    [Fact]
    public async Task SwapFlow_ProposeConsentActivate_MovesBothSidesAndRaisesOneEvent()
    {
        var primary = TestTimekeeping.Assignment(_tenantId, "emp-1");
        var secondary = TestTimekeeping.Assignment(_tenantId, "emp-2");
        _repository.GetByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);
        _repository.GetByIdAsync(secondary.Id, Arg.Any<CancellationToken>()).Returns(secondary);

        await new ProposeShiftSwapCommandHandler(_repository).Handle(
            new ProposeShiftSwapCommand(_tenantId, primary.Id.Value, secondary.Id.Value, Today, Guid.NewGuid()),
            CancellationToken.None);

        await new ConsentToShiftSwapCommandHandler(_repository).Handle(
            new ConsentToShiftSwapCommand(_tenantId, primary.Id.Value, "emp-1"), CancellationToken.None);
        await new ConsentToShiftSwapCommandHandler(_repository).Handle(
            new ConsentToShiftSwapCommand(_tenantId, primary.Id.Value, "emp-2"), CancellationToken.None);

        var activated = await new ActivateShiftSwapCommandHandler(_repository, Clock).Handle(
            new ActivateShiftSwapCommand(_tenantId, primary.Id.Value), CancellationToken.None);

        activated.IsSuccess.Should().BeTrue();
        primary.Status.Should().Be(ShiftAssignmentStatus.Swapped);
        secondary.Status.Should().Be(ShiftAssignmentStatus.Swapped);
        primary.SwapLinkedAssignmentId.Should().Be(secondary.Id);
        secondary.SwapLinkedAssignmentId.Should().Be(primary.Id);
        primary.DomainEvents.OfType<ShiftAssignmentSwapped>().Should().ContainSingle();
    }

    /// <summary>TK-034: neither side moves while only one party has agreed.</summary>
    [Fact]
    public async Task SwapFlow_Activate_Fails_AndLeavesBothSidesUnchanged_WithOneConsent()
    {
        var primary = TestTimekeeping.Assignment(_tenantId, "emp-1");
        var secondary = TestTimekeeping.Assignment(_tenantId, "emp-2");
        _repository.GetByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);
        _repository.GetByIdAsync(secondary.Id, Arg.Any<CancellationToken>()).Returns(secondary);

        await new ProposeShiftSwapCommandHandler(_repository).Handle(
            new ProposeShiftSwapCommand(_tenantId, primary.Id.Value, secondary.Id.Value, Today, Guid.NewGuid()),
            CancellationToken.None);
        await new ConsentToShiftSwapCommandHandler(_repository).Handle(
            new ConsentToShiftSwapCommand(_tenantId, primary.Id.Value, "emp-1"), CancellationToken.None);

        var activated = await new ActivateShiftSwapCommandHandler(_repository, Clock).Handle(
            new ActivateShiftSwapCommand(_tenantId, primary.Id.Value), CancellationToken.None);

        activated.Error.Should().Be(TimekeepingErrors.SwapRequiresBothPartiesConsent);
        primary.Status.Should().Be(ShiftAssignmentStatus.Scheduled);
        secondary.Status.Should().Be(ShiftAssignmentStatus.Scheduled);
    }

    [Fact]
    public async Task SwapFlow_OverrideSubstitutesForConsent()
    {
        var primary = TestTimekeeping.Assignment(_tenantId, "emp-1");
        var secondary = TestTimekeeping.Assignment(_tenantId, "emp-2");
        _repository.GetByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);
        _repository.GetByIdAsync(secondary.Id, Arg.Any<CancellationToken>()).Returns(secondary);

        await new ProposeShiftSwapCommandHandler(_repository).Handle(
            new ProposeShiftSwapCommand(_tenantId, primary.Id.Value, secondary.Id.Value, Today, Guid.NewGuid()),
            CancellationToken.None);
        await new OverrideShiftSwapCommandHandler(_repository).Handle(
            new OverrideShiftSwapCommand(_tenantId, primary.Id.Value, Guid.NewGuid(), "No-show", true),
            CancellationToken.None);

        var activated = await new ActivateShiftSwapCommandHandler(_repository, Clock).Handle(
            new ActivateShiftSwapCommand(_tenantId, primary.Id.Value), CancellationToken.None);

        activated.IsSuccess.Should().BeTrue();
        primary.DomainEvents.OfType<ShiftAssignmentSwapped>().Single().BothPartiesConsented.Should().BeFalse();
    }

    [Fact]
    public async Task Activate_Fails_WhenNoSwapWasProposed()
    {
        var primary = TestTimekeeping.Assignment(_tenantId, "emp-1");
        _repository.GetByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);

        var result = await new ActivateShiftSwapCommandHandler(_repository, Clock).Handle(
            new ActivateShiftSwapCommand(_tenantId, primary.Id.Value), CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.SwapRequiresBothPartiesConsent);
    }

    /// <summary>TK-032: expiry runs without administrative action.</summary>
    [Fact]
    public async Task ExpireDue_ExpiresEveryAssignmentPastItsEndDate()
    {
        var first = TestTimekeeping.Assignment(_tenantId, "emp-1", from: Today.AddDays(-30), to: Today.AddDays(-1));
        var second = TestTimekeeping.Assignment(_tenantId, "emp-2", from: Today.AddDays(-30), to: Today.AddDays(-1));
        _repository.ListExpirableAsync(Today, Arg.Any<CancellationToken>()).Returns([first, second]);
        var handler = new ExpireDueShiftAssignmentsCommandHandler(_repository, Clock);

        var result = await handler.Handle(new ExpireDueShiftAssignmentsCommand(Today), CancellationToken.None);

        result.Value.Should().Be(2);
        first.Status.Should().Be(ShiftAssignmentStatus.Expired);
        second.Status.Should().Be(ShiftAssignmentStatus.Expired);
        first.DomainEvents.OfType<ShiftAssignmentExpired>().Should().ContainSingle();
    }
}

public sealed class HolidayCalendarCommandHandlerTests
{
    private readonly IHolidayCalendarRepository _repository = Substitute.For<IHolidayCalendarRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;
    private static FakeTimeProvider Clock => new(TestTimekeeping.NowUtc);

    /// <summary>TK-041 enforced at the command boundary as well as in the aggregate.</summary>
    [Fact]
    public async Task Define_Fails_WhenATenantAttemptsACountryLevelCalendar()
    {
        var handler = new DefineHolidayCalendarCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new DefineHolidayCalendarCommand(
                _tenantId, "PH", HolidayCalendarLevel.Country, "PH", "PH", null, Today, false, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.CountryLayerIsReadOnlyToTenant);
    }

    [Fact]
    public async Task Define_StoresACountryCalendarWithNoOwningTenant_WhenActingAsPlatform()
    {
        var handler = new DefineHolidayCalendarCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new DefineHolidayCalendarCommand(
                _tenantId, "PH", HolidayCalendarLevel.Country, "PH", "PH", null, Today, true, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<HolidayCalendar>(c => c.TenantId == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Define_StoresACompanyCalendarAgainstItsTenant()
    {
        var handler = new DefineHolidayCalendarCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new DefineHolidayCalendarCommand(
                _tenantId, "Acme", HolidayCalendarLevel.Company, "acme", "PH", Guid.NewGuid(), Today, false,
                Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<HolidayCalendar>(c => c.TenantId == _tenantId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddHoliday_Succeeds_OnADraftCompanyCalendar()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(60));
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);
        _repository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var handler = new AddHolidayCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new AddHolidayCommand(
                _tenantId, company.Id.Value, Today, "Founders Day", HolidayType.CompanyHoliday,
                HolidayWorkRule.NoWorkExpected, false, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        company.Holidays.Should().ContainSingle();
    }

    /// <summary>
    /// A country calendar has no tenant, so tenant-scoped lookup must still find it —
    /// otherwise the national layer would be invisible to everyone.
    /// </summary>
    [Fact]
    public async Task AddHoliday_ReachesTheTenantlessCountryCalendar()
    {
        var country = TestTimekeeping.CountryCalendar();
        _repository.GetByIdAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);
        var handler = new AddHolidayCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new AddHolidayCommand(
                _tenantId, country.Id.Value, Today, "Independence Day", HolidayType.RegularHoliday,
                HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddHoliday_ReturnsNotFound_ForAnotherTenantsCalendar()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(60));
        var other = TestTimekeeping.CompanyCalendar(Guid.NewGuid(), country.Id);
        _repository.GetByIdAsync(other.Id, Arg.Any<CancellationToken>()).Returns(other);
        var handler = new AddHolidayCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new AddHolidayCommand(
                _tenantId, other.Id.Value, Today, "X", HolidayType.CompanyHoliday, HolidayWorkRule.NoWorkExpected,
                false, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(TimekeepingErrors.HolidayCalendarNotFound);
    }

    [Fact]
    public async Task RemoveHoliday_And_Publish_Succeed()
    {
        var country = TestTimekeeping.CountryCalendar();
        var keep = country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Keep", HolidayType.RegularHoliday, HolidayWorkRule.NoWorkExpected,
            true, Guid.NewGuid(), TestTimekeeping.NowUtc).Value;
        var drop = country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today.AddDays(1), "Drop", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc).Value;
        _repository.GetByIdAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);

        var removed = await new RemoveHolidayCommandHandler(_repository, Clock).Handle(
            new RemoveHolidayCommand(_tenantId, country.Id.Value, drop.Value, true, Guid.NewGuid()),
            CancellationToken.None);
        removed.IsSuccess.Should().BeTrue();

        var published = await new PublishHolidayCalendarCommandHandler(_repository, Clock).Handle(
            new PublishHolidayCalendarCommand(_tenantId, country.Id.Value, Guid.NewGuid()), CancellationToken.None);

        published.IsSuccess.Should().BeTrue();
        country.Holidays.Should().ContainSingle().Which.Id.Should().Be(keep);
    }

    [Fact]
    public async Task Revise_AddsTheNextVersion()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        _repository.GetByIdAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);
        var handler = new ReviseHolidayCalendarCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new ReviseHolidayCalendarCommand(_tenantId, country.Id.Value, Today.AddDays(60), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<HolidayCalendar>(c => c.Version == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeLayer_Succeeds()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        var otherCountry = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(2));
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);
        _repository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var handler = new ChangeHolidayCalendarLayerCommandHandler(_repository, Clock);

        var result = await handler.Handle(
            new ChangeHolidayCalendarLayerCommand(
                _tenantId, company.Id.Value, otherCountry.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        company.ParentCalendarId.Should().Be(otherCountry.Id);
    }
}
