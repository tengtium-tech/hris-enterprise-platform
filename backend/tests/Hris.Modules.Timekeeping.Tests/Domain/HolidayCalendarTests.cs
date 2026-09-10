using FluentAssertions;
using Hris.Modules.Timekeeping.Domain;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

public sealed class HolidayCalendarTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    [Fact]
    public void Create_Fails_WhenACountryCalendarDeclaresAParent()
    {
        var result = HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), null, "PH", HolidayCalendarLevel.Country, "PH", "PH",
            new HolidayCalendarId(Guid.NewGuid()), Today, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.ParentCalendarProhibitedForCountryLayer);
    }

    /// <summary>
    /// Without a parent, a company calendar's resolved set would silently omit the
    /// statutory holidays it is supposed to layer on top of.
    /// </summary>
    [Fact]
    public void Create_Fails_WhenANonCountryCalendarHasNoParent()
    {
        var result = HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), _tenantId, "Acme", HolidayCalendarLevel.Company, "acme", "PH", null,
            Today, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.ParentCalendarRequiredForNonCountryLayer);
    }

    [Fact]
    public void Create_Fails_WhenNameOrScopeIsMissing()
    {
        HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), null, " ", HolidayCalendarLevel.Country, "PH", "PH", null, Today,
            Guid.NewGuid(), TestTimekeeping.NowUtc).Error.Should().Be(TimekeepingErrors.HolidayCalendarNameRequired);

        HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), null, "PH", HolidayCalendarLevel.Country, " ", "PH", null, Today,
            Guid.NewGuid(), TestTimekeeping.NowUtc).Error.Should().Be(TimekeepingErrors.HolidayCalendarScopeRequired);
    }

    /// <summary>
    /// TK-041. The Philippine statutory calendar is published by proclamation and is
    /// identical for every employer; a tenant transcribing it independently would
    /// reproduce the correctness and liability problems statutory reference data
    /// exists to avoid.
    /// </summary>
    [Fact]
    public void AddHoliday_Fails_WhenATenantTriesToEditTheCountryLayer()
    {
        var country = TestTimekeeping.CountryCalendar();

        var result = country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Tenant Holiday", HolidayType.CompanyHoliday,
            HolidayWorkRule.NoWorkExpected, false, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.CountryLayerIsReadOnlyToTenant);
    }

    [Fact]
    public void RemoveHoliday_Fails_WhenATenantTriesToEditTheCountryLayer()
    {
        var country = TestTimekeeping.CountryCalendar();
        var holidayId = country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Independence Day", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc).Value;

        country.RemoveHoliday(holidayId, false, Guid.NewGuid(), TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.CountryLayerIsReadOnlyToTenant);
    }

    [Fact]
    public void AddHoliday_Succeeds_WhenThePlatformEditsTheCountryLayer()
    {
        var country = TestTimekeeping.CountryCalendar();

        var result = country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Independence Day", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        country.DomainEvents.OfType<HolidayAdded>().Should().ContainSingle();
    }

    /// <summary>
    /// TK-043. A company calendar declaring one of its dates a Regular Holiday would
    /// be asserting statutory status the tenant has no authority to confer.
    /// </summary>
    [Fact]
    public void AddHoliday_Fails_WhenAStatutoryTypeIsUsedOutsideTheCountryLayer()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(30));
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);

        var result = company.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Fake Statutory", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, false, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.Error.Should().Be(TimekeepingErrors.StatutoryHolidayTypeRequiresCountryLayer);
    }

    [Fact]
    public void AddHoliday_Succeeds_ForACompanyTypeOnACompanyCalendar()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(30));
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);

        var result = company.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Founders Day", HolidayType.CompanyHoliday,
            HolidayWorkRule.NoWorkExpected, false, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddHoliday_Fails_OnADuplicateDate()
    {
        var country = TestTimekeeping.CountryCalendar();
        country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "First", HolidayType.RegularHoliday, HolidayWorkRule.NoWorkExpected,
            true, Guid.NewGuid(), TestTimekeeping.NowUtc);

        var second = country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Second", HolidayType.RegularHoliday, HolidayWorkRule.NoWorkExpected,
            true, Guid.NewGuid(), TestTimekeeping.NowUtc);

        second.Error.Should().Be(TimekeepingErrors.DuplicateHolidayDate);
    }

    [Fact]
    public void AddHoliday_Fails_WhenTheNameIsMissing()
    {
        var country = TestTimekeeping.CountryCalendar();

        country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, " ", HolidayType.RegularHoliday, HolidayWorkRule.NoWorkExpected,
            true, Guid.NewGuid(), TestTimekeeping.NowUtc).Error.Should().Be(TimekeepingErrors.HolidayNameRequired);
    }

    [Fact]
    public void AddHoliday_Fails_OnAPublishedCalendar()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);

        country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today.AddDays(1), "Later", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.CalendarNotDraft);
    }

    [Fact]
    public void RemoveHoliday_RemovesIt_AndRaisesTheEvent()
    {
        var country = TestTimekeeping.CountryCalendar();
        var holidayId = country.AddHoliday(
            new HolidayId(Guid.NewGuid()), Today, "Independence Day", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), TestTimekeeping.NowUtc).Value;

        var result = country.RemoveHoliday(holidayId, true, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        country.Holidays.Should().BeEmpty();
        country.DomainEvents.OfType<HolidayRemoved>().Should().ContainSingle();
    }

    [Fact]
    public void RemoveHoliday_Fails_WhenTheHolidayIsNotInThisCalendar()
    {
        var country = TestTimekeeping.CountryCalendar();

        country.RemoveHoliday(new HolidayId(Guid.NewGuid()), true, Guid.NewGuid(), TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.HolidayNotFound);
    }

    [Fact]
    public void Publish_Fails_WhenTheCalendarHasNoEntries()
    {
        var country = TestTimekeeping.CountryCalendar();

        country.Publish(Guid.NewGuid(), TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.CalendarHasNoHolidays);
    }

    [Fact]
    public void Publish_MakesItPublished_AndRaisesTheEvent()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);

        country.Status.Should().Be(HolidayCalendarStatus.Published);
        country.DomainEvents.OfType<HolidayCalendarPublished>().Should().ContainSingle();
    }

    [Fact]
    public void Supersede_Fails_WhenTheCalendarIsNotPublished()
    {
        var draft = TestTimekeeping.CountryCalendar();

        draft.Supersede(
            new HolidayCalendarId(Guid.NewGuid()), Today.AddDays(30), Guid.NewGuid(), TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.CalendarNotPublished);
    }

    [Fact]
    public void ChangeParentCalendar_RaisesTheLayerChangedEvent()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        var otherCountry = TestTimekeeping.PublishedCountryCalendar(Today.AddDays(2));
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);

        var result = company.ChangeParentCalendar(otherCountry.Id, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        company.ParentCalendarId.Should().Be(otherCountry.Id);
        company.DomainEvents.OfType<HolidayCalendarLayerChanged>().Should().ContainSingle();
    }

    [Fact]
    public void ChangeParentCalendar_IsANoOp_WhenTheParentIsUnchanged()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);

        var result = company.ChangeParentCalendar(country.Id, Guid.NewGuid(), TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        company.DomainEvents.OfType<HolidayCalendarLayerChanged>().Should().BeEmpty();
    }

    [Fact]
    public void ChangeParentCalendar_Fails_WhenRemovingTheParentOfANonCountryCalendar()
    {
        var country = TestTimekeeping.PublishedCountryCalendar(Today);
        var company = TestTimekeeping.CompanyCalendar(_tenantId, country.Id);

        company.ChangeParentCalendar(null, Guid.NewGuid(), TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.ParentCalendarRequiredForNonCountryLayer);
    }

    [Fact]
    public void CountryCalendar_HasNoOwningTenant()
    {
        TestTimekeeping.CountryCalendar().TenantId.Should().BeNull(
            "the statutory layer is platform data every tenant in that country reads");
    }
}
