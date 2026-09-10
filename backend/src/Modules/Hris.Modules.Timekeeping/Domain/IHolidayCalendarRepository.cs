namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Repository contract for the <see cref="HolidayCalendar"/> Aggregate Root. There is
/// deliberately no repository for <see cref="Holiday"/> — it is reached through its
/// calendar (CTR-ARC-004).
/// </summary>
public interface IHolidayCalendarRepository
{
    Task<HolidayCalendar?> GetByIdAsync(HolidayCalendarId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<HolidayCalendar>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HolidayCalendar>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    /// <summary>
    /// A calendar and every ancestor it layers onto, as the full version history of
    /// each. This is the input <see cref="HolidayResolver"/> needs: it filters to the
    /// versions effective on the evaluated date itself, so the caller does not have to
    /// get TK-002 right independently.
    /// </summary>
    Task<IReadOnlyList<HolidayCalendar>> ListLayerChainAsync(
        HolidayCalendarId leafCalendarId, CancellationToken cancellationToken);

    Task AddAsync(HolidayCalendar calendar, CancellationToken cancellationToken);
}
