using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IHolidayCalendarRepository"/>.</summary>
internal sealed class HolidayCalendarRepository : IHolidayCalendarRepository
{
    private readonly HrisDbContext _dbContext;

    public HolidayCalendarRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<HolidayCalendar?> GetByIdAsync(HolidayCalendarId id, CancellationToken cancellationToken) =>
        _dbContext.Set<HolidayCalendar>().FirstOrDefaultAsync(calendar => calendar.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HolidayCalendar>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<HolidayCalendar>()
            .Where(calendar => calendar.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<HolidayCalendar>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken) =>
        await _dbContext.Set<HolidayCalendar>()
            .Where(calendar => calendar.LineageId == lineageId)
            .OrderBy(calendar => calendar.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Walks from the leaf calendar up through its parent links, returning every
    /// version of every layer. Bounded by a maximum depth so a
    /// parent-link cycle introduced by bad data cannot spin here forever — the
    /// documented layering is only ever three deep.
    /// </summary>
    public async Task<IReadOnlyList<HolidayCalendar>> ListLayerChainAsync(
        HolidayCalendarId leafCalendarId, CancellationToken cancellationToken)
    {
        var chain = new List<HolidayCalendar>();
        var visited = new HashSet<Guid>();
        HolidayCalendarId? current = leafCalendarId;

        for (var depth = 0; current is not null && depth < _maximumLayerDepth; depth++)
        {
            var calendar = await GetByIdAsync(current.Value, cancellationToken).ConfigureAwait(false);
            if (calendar is null || !visited.Add(calendar.LineageId))
            {
                break;
            }

            var versions = await ListByLineageAsync(calendar.LineageId, cancellationToken).ConfigureAwait(false);
            chain.AddRange(versions);

            current = calendar.ParentCalendarId;
        }

        return chain;
    }

    public async Task AddAsync(HolidayCalendar calendar, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(calendar, nameof(calendar));
        await _dbContext.Set<HolidayCalendar>().AddAsync(calendar, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Country, Region, Company — three, with headroom.</summary>
    private const int _maximumLayerDepth = 8;
}
