using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Application;

/// <summary>
/// The one place every handler that loads an aggregate by identifier performs this
/// module's tenant-isolation check, per CTR-ISO-004. Returning not-found for both a
/// genuinely missing record and one belonging to another tenant is deliberate
/// (CTR-ISO-002): a forbidden response would confirm the identifier exists
/// somewhere, which is the enumeration signal that requirement removes.
///
/// <see cref="LoadCalendarForTenantAsync"/> is the one exception, and deliberately
/// so: a Country-level calendar has no tenant at all, because it is
/// platform-provided statutory data every tenant in that country reads. Rejecting it
/// on a tenant mismatch would make the national holiday layer invisible to everyone.
/// </summary>
internal static class TimekeepingLookup
{
    public static async Task<Result<WorkSchedule>> LoadScheduleForTenantAsync(
        IWorkScheduleRepository repository, Guid scheduleId, Guid tenantId, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetByIdAsync(new WorkScheduleId(scheduleId), cancellationToken)
            .ConfigureAwait(false);

        return schedule is null || schedule.TenantId != tenantId
            ? Result.Failure<WorkSchedule>(TimekeepingErrors.WorkScheduleNotFound)
            : Result.Success(schedule);
    }

    public static async Task<Result<WorkShift>> LoadShiftForTenantAsync(
        IWorkShiftRepository repository, Guid shiftId, Guid tenantId, CancellationToken cancellationToken)
    {
        var shift = await repository.GetByIdAsync(new WorkShiftId(shiftId), cancellationToken).ConfigureAwait(false);

        return shift is null || shift.TenantId != tenantId
            ? Result.Failure<WorkShift>(TimekeepingErrors.WorkShiftNotFound)
            : Result.Success(shift);
    }

    public static async Task<Result<ShiftAssignment>> LoadAssignmentForTenantAsync(
        IShiftAssignmentRepository repository, Guid assignmentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetByIdAsync(new ShiftAssignmentId(assignmentId), cancellationToken)
            .ConfigureAwait(false);

        return assignment is null || assignment.TenantId != tenantId
            ? Result.Failure<ShiftAssignment>(TimekeepingErrors.ShiftAssignmentNotFound)
            : Result.Success(assignment);
    }

    /// <summary>
    /// Accepts a calendar whose <c>TenantId</c> is null, which is how the
    /// platform-provided Country layer is stored. Every other calendar must match the
    /// requesting tenant.
    /// </summary>
    public static async Task<Result<HolidayCalendar>> LoadCalendarForTenantAsync(
        IHolidayCalendarRepository repository, Guid calendarId, Guid tenantId, CancellationToken cancellationToken)
    {
        var calendar = await repository.GetByIdAsync(new HolidayCalendarId(calendarId), cancellationToken)
            .ConfigureAwait(false);

        if (calendar is null)
        {
            return Result.Failure<HolidayCalendar>(TimekeepingErrors.HolidayCalendarNotFound);
        }

        return calendar.TenantId is not null && calendar.TenantId != tenantId
            ? Result.Failure<HolidayCalendar>(TimekeepingErrors.HolidayCalendarNotFound)
            : Result.Success(calendar);
    }
}
