using FluentAssertions;
using Hris.Modules.Timekeeping.Domain;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

/// <summary>
/// TK-030: resolving the applicable shift assignment always yields exactly one
/// answer or an explicit unresolved state, never a silent pick.
///
/// business-rules.md names the specific wrong implementation this guards against:
/// taking "the first matching assignment found" when precedence is ambiguous makes
/// the result depend on data-access order rather than on a business rule, so the
/// same question can return different answers depending on how rows came back.
/// </summary>
public sealed class ShiftAssignmentResolverTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    [Fact]
    public void Resolve_ReturnsUnresolved_WhenNoAssignmentApplies()
    {
        var resolution = ShiftAssignmentResolver.Resolve([], Today);

        resolution.IsUnresolved.Should().BeTrue();
        resolution.IsAmbiguous.Should().BeFalse();
        resolution.Assignment.Should().BeNull();
    }

    [Fact]
    public void Resolve_ReturnsTheOnlyApplicableAssignment()
    {
        var assignment = TestTimekeeping.Assignment(_tenantId, "emp-1");

        var resolution = ShiftAssignmentResolver.Resolve([assignment], Today);

        resolution.Assignment.Should().Be(assignment);
        resolution.IsUnresolved.Should().BeFalse();
    }

    /// <summary>The precedence rule itself: most specific level wins.</summary>
    [Theory]
    [InlineData(OrganizationalAssignmentLevel.Company)]
    [InlineData(OrganizationalAssignmentLevel.LegalEntity)]
    [InlineData(OrganizationalAssignmentLevel.BusinessUnit)]
    [InlineData(OrganizationalAssignmentLevel.Department)]
    [InlineData(OrganizationalAssignmentLevel.EmployeeGroup)]
    [InlineData(OrganizationalAssignmentLevel.EmploymentType)]
    [InlineData(OrganizationalAssignmentLevel.Position)]
    public void Resolve_PrefersTheIndividualAssignment_OverEveryBroaderLevel(OrganizationalAssignmentLevel broaderLevel)
    {
        var individual = TestTimekeeping.Assignment(_tenantId, "emp-1");
        var broader = TestTimekeeping.Assignment(_tenantId, "unit-1", broaderLevel);

        var resolution = ShiftAssignmentResolver.Resolve([broader, individual], Today);

        resolution.Assignment.Should().Be(individual);
    }

    [Fact]
    public void Resolve_PrefersDepartment_OverBusinessUnit()
    {
        var department = TestTimekeeping.Assignment(_tenantId, "dept-1", OrganizationalAssignmentLevel.Department);
        var businessUnit = TestTimekeeping.Assignment(_tenantId, "bu-1", OrganizationalAssignmentLevel.BusinessUnit);

        var resolution = ShiftAssignmentResolver.Resolve([businessUnit, department], Today);

        resolution.Assignment.Should().Be(department);
    }

    /// <summary>
    /// The case the rule exists for. Two assignments at the same level both apply, so
    /// resolution refuses rather than picking whichever the query returned first.
    /// </summary>
    [Fact]
    public void Resolve_ReturnsAmbiguous_WhenTwoAssignmentsTieAtTheSameLevel()
    {
        var first = TestTimekeeping.Assignment(_tenantId, "dept-1", OrganizationalAssignmentLevel.Department);
        var second = TestTimekeeping.Assignment(_tenantId, "dept-2", OrganizationalAssignmentLevel.Department);

        var resolution = ShiftAssignmentResolver.Resolve([first, second], Today);

        resolution.IsAmbiguous.Should().BeTrue();
        resolution.Assignment.Should().BeNull();
    }

    [Fact]
    public void Resolve_IsIndependentOfCandidateOrder()
    {
        var individual = TestTimekeeping.Assignment(_tenantId, "emp-1");
        var department = TestTimekeeping.Assignment(_tenantId, "dept-1", OrganizationalAssignmentLevel.Department);

        var forward = ShiftAssignmentResolver.Resolve([individual, department], Today);
        var reversed = ShiftAssignmentResolver.Resolve([department, individual], Today);

        forward.Assignment.Should().Be(reversed.Assignment);
    }

    [Fact]
    public void Resolve_IgnoresAssignmentsOutsideTheirEffectivePeriod()
    {
        var expiredAssignment = TestTimekeeping.Assignment(
            _tenantId, "emp-1", from: Today.AddDays(-30), to: Today.AddDays(-1));

        var resolution = ShiftAssignmentResolver.Resolve([expiredAssignment], Today);

        resolution.IsUnresolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_IgnoresACancelledAssignment()
    {
        var assignment = TestTimekeeping.Assignment(_tenantId, "emp-1");
        assignment.Cancel("No longer needed", Guid.NewGuid(), Today, TestTimekeeping.NowUtc);

        var resolution = ShiftAssignmentResolver.Resolve([assignment], Today);

        resolution.IsUnresolved.Should().BeTrue("a cancelled assignment is retained as evidence but expects nothing");
    }

    [Fact]
    public void Resolve_FallsBackToTheBroaderLevel_WhenTheIndividualAssignmentHasEnded()
    {
        var endedIndividual = TestTimekeeping.Assignment(
            _tenantId, "emp-1", from: Today.AddDays(-30), to: Today.AddDays(-1));
        var department = TestTimekeeping.Assignment(_tenantId, "dept-1", OrganizationalAssignmentLevel.Department);

        var resolution = ShiftAssignmentResolver.Resolve([endedIndividual, department], Today);

        resolution.Assignment.Should().Be(department);
    }

    [Fact]
    public void Resolve_Throws_WhenCandidatesAreNull()
    {
        var act = () => ShiftAssignmentResolver.Resolve(null!, Today);

        act.Should().Throw<ArgumentNullException>();
    }
}

/// <summary>
/// TK-042 layering and TK-002 version selection, together — the two properties of
/// holiday resolution that are easy to get wrong independently.
/// </summary>
public sealed class HolidayResolverTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    [Fact]
    public void Resolve_ReturnsNull_WhenNoLayerDeclaresTheDate()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(10));

        HolidayResolver.Resolve([country], Today).Should().BeNull();
    }

    [Fact]
    public void Resolve_ReturnsTheCountryHoliday_WhenOnlyThatLayerDeclaresIt()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);

        var resolution = HolidayResolver.Resolve([country], Today);

        resolution.Should().NotBeNull();
        resolution!.SourceLevel.Should().Be(HolidayCalendarLevel.Country);
        resolution.Holiday.Type.Should().Be(HolidayType.RegularHoliday);
    }

    /// <summary>
    /// TK-042. A company declaring a national holiday a working day for its own staff
    /// is legitimate configuration, and resolution must reflect it rather than
    /// returning the statutory classification because it was found first.
    /// </summary>
    [Fact]
    public void Resolve_PrefersTheMoreSpecificLayer_OnAConflict()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);
        company.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Company Working Day", HolidayType.CompanyHoliday,
            HolidayWorkRule.WorkRequired, false, Guid.NewGuid(), TestTimekeeping.NowUtc);
        company.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);

        var resolution = HolidayResolver.Resolve([country, company], Today);

        resolution!.SourceLevel.Should().Be(HolidayCalendarLevel.Company);
        resolution.Holiday.WorkRule.Should().Be(HolidayWorkRule.WorkRequired);
    }

    [Fact]
    public void Resolve_IsIndependentOfLayerOrder()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);
        company.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Company Working Day", HolidayType.CompanyHoliday,
            HolidayWorkRule.WorkRequired, false, Guid.NewGuid(), TestTimekeeping.NowUtc);
        company.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);

        var forward = HolidayResolver.Resolve([country, company], Today);
        var reversed = HolidayResolver.Resolve([company, country], Today);

        forward!.SourceLevel.Should().Be(reversed!.SourceLevel);
    }

    /// <summary>
    /// TK-002 inside resolution: a version not in force on the evaluated date is
    /// filtered out here rather than by the caller, so passing the full history is
    /// the expected input and a caller cannot get version selection wrong.
    /// </summary>
    [Fact]
    public void Resolve_UsesTheVersionInForceOnTheEvaluatedDate_NotTheCurrentOne()
    {
        var holidayDate = Today.AddDays(5);
        var v1 = TestTimekeeping.PublishedCountryCalendar(holidayDate, Today);

        var v2 = v1.Supersede(
            new HolidayCalendarId(Guid.NewGuid()), Today.AddDays(60), Guid.NewGuid(), TestTimekeeping.NowUtc).Value;
        v2.RemoveHoliday(v2.Holidays[0].Id, true, Guid.NewGuid(), TestTimekeeping.NowUtc);
        v2.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today.AddDays(90), "Later Holiday", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc);
        v2.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);

        var historical = HolidayResolver.Resolve([v1, v2], holidayDate);

        historical.Should().NotBeNull("the version in force on that date still declared it a holiday");
        historical!.SourceCalendarId.Should().Be(v1.Id);
    }

    [Fact]
    public void Resolve_IgnoresADraftCalendar()
    {
        var draft = TestTimekeeping.CountryCalendar();
        draft.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Unpublished", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc);

        HolidayResolver.Resolve([draft], Today).Should().BeNull();
    }

    [Fact]
    public void Resolve_Throws_WhenCalendarsAreNull()
    {
        var act = () => HolidayResolver.Resolve(null!, Today);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsWorkExpected_IsFalse_OnANonWorkingWeekday()
    {
        var pattern = WorkingDayPattern.Create(TestTimekeeping.MondayToFriday).Value;
        var sunday = new DateOnly(2026, 9, 13);

        sunday.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        HolidayResolver.IsWorkExpected(pattern, null, sunday).Should().BeFalse();
    }

    [Fact]
    public void IsWorkExpected_IsFalse_OnAWorkingDayThatIsANoWorkHoliday()
    {
        var pattern = WorkingDayPattern.Create(TestTimekeeping.MondayToFriday).Value;
        var monday = new DateOnly(2026, 9, 14);
        var country = TestTimekeeping.PublishedCountryCalendar(monday);
        var resolution = HolidayResolver.Resolve([country], monday);

        monday.DayOfWeek.Should().Be(DayOfWeek.Monday);
        HolidayResolver.IsWorkExpected(pattern, resolution, monday).Should().BeFalse();
    }

    [Fact]
    public void IsWorkExpected_IsTrue_OnAWorkingDayWhoseHolidayStillRequiresWork()
    {
        var pattern = WorkingDayPattern.Create(TestTimekeeping.MondayToFriday).Value;
        var monday = new DateOnly(2026, 9, 14);
        var country = TestTimekeeping.CountryCalendar();
        country.AddHoliday(
            new HolidayId(Guid.NewGuid()), monday, "Special Working", HolidayType.SpecialWorkingHoliday,
            HolidayWorkRule.WorkRequired, true, Guid.NewGuid(), TestTimekeeping.NowUtc);
        country.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc);

        var resolution = HolidayResolver.Resolve([country], monday);

        HolidayResolver.IsWorkExpected(pattern, resolution, monday).Should().BeTrue();
    }

    [Fact]
    public void IsWorkExpected_Throws_WhenPatternIsNull()
    {
        var act = () => HolidayResolver.IsWorkExpected(null!, null, Today);

        act.Should().Throw<ArgumentNullException>();
    }
}
