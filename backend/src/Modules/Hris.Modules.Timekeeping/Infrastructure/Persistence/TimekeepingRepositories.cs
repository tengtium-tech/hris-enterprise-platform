using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IWorkScheduleRepository"/>.</summary>
internal sealed class WorkScheduleRepository : IWorkScheduleRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkScheduleRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkSchedule?> GetByIdAsync(WorkScheduleId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkSchedule>().FirstOrDefaultAsync(schedule => schedule.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkSchedule>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkSchedule>()
            .Where(schedule => schedule.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkSchedule>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkSchedule>()
            .Where(schedule => schedule.LineageId == lineageId)
            .OrderBy(schedule => schedule.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(schedule, nameof(schedule));
        await _dbContext.Set<WorkSchedule>().AddAsync(schedule, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>EF Core implementation of <see cref="IWorkShiftRepository"/>.</summary>
internal sealed class WorkShiftRepository : IWorkShiftRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkShiftRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkShift?> GetByIdAsync(WorkShiftId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkShift>().FirstOrDefaultAsync(shift => shift.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkShift>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkShift>()
            .Where(shift => shift.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkShift>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkShift>()
            .Where(shift => shift.LineageId == lineageId)
            .OrderBy(shift => shift.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> CodeExistsInTenantAsync(
        Guid tenantId, string code, WorkShiftId? excluding, CancellationToken cancellationToken) =>
        await _dbContext.Set<WorkShift>()
            .Where(shift => shift.TenantId == tenantId)
            .Where(shift => shift.Code.Value == code)
            .Where(shift => excluding == null || shift.Id != excluding)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkShift shift, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(shift, nameof(shift));
        await _dbContext.Set<WorkShift>().AddAsync(shift, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>EF Core implementation of <see cref="IShiftAssignmentRepository"/>.</summary>
internal sealed class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly HrisDbContext _dbContext;

    public ShiftAssignmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<ShiftAssignment?> GetByIdAsync(ShiftAssignmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<ShiftAssignment>().FirstOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ShiftAssignment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ShiftAssignment>> ListByTargetAsync(
        Guid tenantId, string targetId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.TenantId == tenantId)
            .Where(assignment => assignment.TargetId == targetId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ShiftAssignment>> ListIndividualByEmployeeAsync(
        Guid tenantId, string employeeId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.TenantId == tenantId)
            .Where(assignment => assignment.TargetId == employeeId)
            .Where(assignment => assignment.TargetLevel == OrganizationalAssignmentLevel.IndividualEmployee)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ShiftAssignment>> ListExpirableAsync(DateOnly asOfDate, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.EffectiveTo != null && assignment.EffectiveTo < asOfDate)
            .Where(assignment => assignment.Status == ShiftAssignmentStatus.Scheduled
                                 || assignment.Status == ShiftAssignmentStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(assignment, nameof(assignment));
        await _dbContext.Set<ShiftAssignment>().AddAsync(assignment, cancellationToken).ConfigureAwait(false);
    }
}

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
