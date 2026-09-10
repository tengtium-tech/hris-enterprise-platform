using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Aggregate Root representing a dated, layered, versioned set of holidays and the
/// scope it applies to. Source:
/// docs/04-modules/timekeeping/domain/aggregates.md and holiday-calendars.md.
///
/// Layering runs Country to Region to Company, each more specific than the last, and
/// a date's determination for a scope is the union of every applicable layer with
/// the most specific layer's classification governing a conflict (TK-042).
///
/// The Country layer is platform-provided and read-only to the tenant (TK-041). That
/// mirrors statutory-reference-data.md's "statutory rates are law, not
/// configuration" principle applied to holiday dates: the Philippine regular and
/// special holiday calendar for a year is published by government proclamation and
/// is identical for every employer, so letting each tenant transcribe it
/// independently would reproduce exactly the correctness, maintenance, and liability
/// problems that document already argues against for SSS, PhilHealth, Pag-IBIG, and
/// BIR tables.
///
/// <see cref="TenantId"/> is nullable because the platform-provided Country layer is
/// not tenant data at all — it belongs to the platform and is read by every tenant
/// operating in that country.
/// </summary>
public sealed class HolidayCalendar : AggregateRoot<HolidayCalendarId>
{
    private readonly List<Holiday> _holidays = [];

    public Guid? TenantId { get; }

    public Guid LineageId { get; }

    public string Name { get; private set; } = null!;

    public HolidayCalendarLevel Level { get; }

    /// <summary>Country code, region identifier, or organizational unit identifier.</summary>
    public string ScopeTargetId { get; private set; } = null!;

    /// <summary>
    /// The country whose holiday-type vocabulary governs this calendar's entries
    /// (TK-043). Carried on every layer, not only the Country one, so a Company
    /// calendar knows which vocabulary it is validated against.
    /// </summary>
    public string CountryCode { get; private set; } = null!;

    public HolidayCalendarId? ParentCalendarId { get; private set; }

    public IReadOnlyList<Holiday> Holidays => _holidays.AsReadOnly();

    public int Version { get; }

    public DateOnly EffectiveFrom { get; }

    public DateOnly? EffectiveTo { get; private set; }

    public HolidayCalendarStatus Status { get; private set; }

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    private HolidayCalendar(
        HolidayCalendarId id, Guid? tenantId, Guid lineageId, HolidayCalendarLevel level, int version,
        DateOnly effectiveFrom, Guid createdBy, DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        LineageId = lineageId;
        Level = level;
        Version = version;
        EffectiveFrom = effectiveFrom;
        Status = HolidayCalendarStatus.Draft;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    public static Result<HolidayCalendar> Create(
        HolidayCalendarId id, Guid? tenantId, string? name, HolidayCalendarLevel level, string? scopeTargetId,
        string? countryCode, HolidayCalendarId? parentCalendarId, DateOnly effectiveFrom, Guid createdBy,
        DateTimeOffset createdOn) =>
        Build(id, tenantId, id.Value, 1, name, level, scopeTargetId, countryCode, parentCalendarId, effectiveFrom,
            createdBy, createdOn);

    private static Result<HolidayCalendar> Build(
        HolidayCalendarId id, Guid? tenantId, Guid lineageId, int version, string? name, HolidayCalendarLevel level,
        string? scopeTargetId, string? countryCode, HolidayCalendarId? parentCalendarId, DateOnly effectiveFrom,
        Guid createdBy, DateTimeOffset createdOn)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<HolidayCalendar>(TimekeepingErrors.HolidayCalendarNameRequired);
        }

        if (string.IsNullOrWhiteSpace(scopeTargetId) || string.IsNullOrWhiteSpace(countryCode))
        {
            return Result.Failure<HolidayCalendar>(TimekeepingErrors.HolidayCalendarScopeRequired);
        }

        // A country calendar is the base layer and has no parent; anything above it
        // layers onto something, or its resolved set would silently omit the
        // statutory holidays it is supposed to build on.
        if (level == HolidayCalendarLevel.Country && parentCalendarId is not null)
        {
            return Result.Failure<HolidayCalendar>(TimekeepingErrors.ParentCalendarProhibitedForCountryLayer);
        }

        if (level != HolidayCalendarLevel.Country && parentCalendarId is null)
        {
            return Result.Failure<HolidayCalendar>(TimekeepingErrors.ParentCalendarRequiredForNonCountryLayer);
        }

        var calendar = new HolidayCalendar(id, tenantId, lineageId, level, version, effectiveFrom, createdBy, createdOn)
        {
            Name = name.Trim(),
            ScopeTargetId = scopeTargetId.Trim(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            ParentCalendarId = parentCalendarId,
        };

        return Result.Success(calendar);
    }

    /// <summary>
    /// TK-041 and TK-043 both apply here. A tenant may not add to the
    /// platform-provided Country layer at all, and a statutory classification may be
    /// used only within a Country-level calendar — a Company calendar declaring one
    /// of its dates a Regular Holiday would be asserting statutory status the tenant
    /// has no authority to confer.
    /// </summary>
    public Result<HolidayId> AddHoliday(
        HolidayId holidayId, DateOnly date, string? name, HolidayType type, HolidayWorkRule workRule,
        bool actingAsPlatform, Guid changedBy, DateTimeOffset nowUtc)
    {
        if (Status == HolidayCalendarStatus.Superseded)
        {
            return Result.Failure<HolidayId>(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        if (Status != HolidayCalendarStatus.Draft)
        {
            return Result.Failure<HolidayId>(TimekeepingErrors.CalendarNotDraft);
        }

        if (Level == HolidayCalendarLevel.Country && !actingAsPlatform)
        {
            return Result.Failure<HolidayId>(TimekeepingErrors.CountryLayerIsReadOnlyToTenant);
        }

        if (Holiday.IsStatutoryType(type) && Level != HolidayCalendarLevel.Country)
        {
            return Result.Failure<HolidayId>(TimekeepingErrors.StatutoryHolidayTypeRequiresCountryLayer);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<HolidayId>(TimekeepingErrors.HolidayNameRequired);
        }

        if (_holidays.Any(existing => existing.Date == date))
        {
            return Result.Failure<HolidayId>(TimekeepingErrors.DuplicateHolidayDate);
        }

        _holidays.Add(new Holiday(holidayId, date, name.Trim(), type, workRule));

        AddDomainEvent(new HolidayAdded(
            Guid.NewGuid(), nowUtc, Id, TenantId, holidayId, date, type, workRule, changedBy));

        return Result.Success(holidayId);
    }

    public Result RemoveHoliday(HolidayId holidayId, bool actingAsPlatform, Guid changedBy, DateTimeOffset nowUtc)
    {
        if (Status == HolidayCalendarStatus.Superseded)
        {
            return Result.Failure(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        if (Status != HolidayCalendarStatus.Draft)
        {
            return Result.Failure(TimekeepingErrors.CalendarNotDraft);
        }

        if (Level == HolidayCalendarLevel.Country && !actingAsPlatform)
        {
            return Result.Failure(TimekeepingErrors.CountryLayerIsReadOnlyToTenant);
        }

        var holiday = _holidays.Find(candidate => candidate.Id == holidayId);
        if (holiday is null)
        {
            return Result.Failure(TimekeepingErrors.HolidayNotFound);
        }

        _holidays.Remove(holiday);

        AddDomainEvent(new HolidayRemoved(Guid.NewGuid(), nowUtc, Id, TenantId, holidayId, holiday.Date, changedBy));

        return Result.Success();
    }

    public Result Publish(Guid publishedBy, DateTimeOffset nowUtc)
    {
        if (Status == HolidayCalendarStatus.Superseded)
        {
            return Result.Failure(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        if (Status != HolidayCalendarStatus.Draft)
        {
            return Result.Failure(TimekeepingErrors.CalendarNotDraft);
        }

        if (_holidays.Count == 0)
        {
            return Result.Failure(TimekeepingErrors.CalendarHasNoHolidays);
        }

        Status = HolidayCalendarStatus.Published;
        AddDomainEvent(new HolidayCalendarPublished(
            Guid.NewGuid(), nowUtc, Id, TenantId, Version, EffectiveFrom, publishedBy));

        return Result.Success();
    }

    /// <summary>
    /// Produces the next version as a Draft carrying a copy of this version's
    /// entries, leaving this one unedited (TK-044). The copy is what makes revising a
    /// calendar practical without re-entering a year of holidays.
    /// </summary>
    public Result<HolidayCalendar> Supersede(
        HolidayCalendarId newId, DateOnly newEffectiveFrom, Guid createdBy, DateTimeOffset nowUtc)
    {
        if (Status != HolidayCalendarStatus.Published)
        {
            return Result.Failure<HolidayCalendar>(TimekeepingErrors.CalendarNotPublished);
        }

        if (newEffectiveFrom <= EffectiveFrom)
        {
            return Result.Failure<HolidayCalendar>(TimekeepingErrors.EffectiveFromNotAfterCurrentVersion);
        }

        var nextResult = Build(
            newId, TenantId, LineageId, Version + 1, Name, Level, ScopeTargetId, CountryCode, ParentCalendarId,
            newEffectiveFrom, createdBy, nowUtc);
        if (nextResult.IsFailure)
        {
            return nextResult;
        }

        var next = nextResult.Value;
        foreach (var holiday in _holidays)
        {
            next._holidays.Add(new Holiday(
                new HolidayId(Guid.NewGuid()), holiday.Date, holiday.Name, holiday.Type, holiday.WorkRule));
        }

        Status = HolidayCalendarStatus.Superseded;
        EffectiveTo = newEffectiveFrom.AddDays(-1);

        next.AddDomainEvent(new HolidayCalendarSuperseded(
            Guid.NewGuid(), nowUtc, next.Id, TenantId, Version, next.Version, newEffectiveFrom));

        return Result.Success(next);
    }

    public Result ChangeParentCalendar(HolidayCalendarId? newParentCalendarId, Guid changedBy, DateTimeOffset nowUtc)
    {
        if (Status == HolidayCalendarStatus.Superseded)
        {
            return Result.Failure(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        if (Level == HolidayCalendarLevel.Country && newParentCalendarId is not null)
        {
            return Result.Failure(TimekeepingErrors.ParentCalendarProhibitedForCountryLayer);
        }

        if (Level != HolidayCalendarLevel.Country && newParentCalendarId is null)
        {
            return Result.Failure(TimekeepingErrors.ParentCalendarRequiredForNonCountryLayer);
        }

        if (newParentCalendarId == ParentCalendarId)
        {
            return Result.Success();
        }

        var previous = ParentCalendarId;
        ParentCalendarId = newParentCalendarId;

        AddDomainEvent(new HolidayCalendarLayerChanged(
            Guid.NewGuid(), nowUtc, Id, TenantId, previous, newParentCalendarId, EffectiveFrom, changedBy));

        return Result.Success();
    }

    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || date <= EffectiveTo.Value);

    public Holiday? HolidayOn(DateOnly date) => _holidays.Find(holiday => holiday.Date == date);
}
