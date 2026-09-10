namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Repository contract for the <see cref="WorkSchedule"/> Aggregate Root. There is
/// deliberately no repository for <see cref="ScheduleAssignment"/> — it is reached
/// through its schedule (CTR-ARC-004).
/// </summary>
public interface IWorkScheduleRepository
{
    Task<WorkSchedule?> GetByIdAsync(WorkScheduleId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkSchedule>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Every version in one lineage. TK-002 resolution needs the full history, not
    /// only the current version, so a query about a past date can select the version
    /// that was in force then.
    /// </summary>
    Task<IReadOnlyList<WorkSchedule>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken);
}

/// <summary>Repository contract for the <see cref="WorkShift"/> Aggregate Root.</summary>
public interface IWorkShiftRepository
{
    Task<WorkShift?> GetByIdAsync(WorkShiftId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkShift>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkShift>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken);

    /// <summary>Supports the tenant-uniqueness check a shift code carries.</summary>
    Task<bool> CodeExistsInTenantAsync(
        Guid tenantId, string code, WorkShiftId? excluding, CancellationToken cancellationToken);

    Task AddAsync(WorkShift shift, CancellationToken cancellationToken);
}

/// <summary>Repository contract for the <see cref="ShiftAssignment"/> Aggregate Root.</summary>
public interface IShiftAssignmentRepository
{
    Task<ShiftAssignment?> GetByIdAsync(ShiftAssignmentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ShiftAssignment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Every assignment naming <paramref name="targetId"/> at any level. This is the
    /// candidate set <see cref="ShiftAssignmentResolver"/> consumes, and the reason
    /// resolution can be a pure function: the query gathers, the resolver decides.
    /// </summary>
    Task<IReadOnlyList<ShiftAssignment>> ListByTargetAsync(
        Guid tenantId, string targetId, CancellationToken cancellationToken);

    /// <summary>
    /// Individual-level assignments for one employee, used for TK-031's overlap check
    /// before a new individual assignment is accepted.
    /// </summary>
    Task<IReadOnlyList<ShiftAssignment>> ListIndividualByEmployeeAsync(
        Guid tenantId, string employeeId, CancellationToken cancellationToken);

    /// <summary>
    /// Assignments whose end date has passed and which have not yet been expired.
    /// Drives TK-032's automatic expiry, which runs without administrative action.
    /// </summary>
    Task<IReadOnlyList<ShiftAssignment>> ListExpirableAsync(DateOnly asOfDate, CancellationToken cancellationToken);

    Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken);
}

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
