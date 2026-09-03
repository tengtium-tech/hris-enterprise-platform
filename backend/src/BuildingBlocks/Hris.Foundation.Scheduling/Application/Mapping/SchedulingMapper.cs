using Hris.Foundation.Scheduling.Application.Dtos;
using Hris.Foundation.Scheduling.Domain;

namespace Hris.Foundation.Scheduling.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4 framework's own mapper already establishes.
/// </summary>
internal static class SchedulingMapper
{
    public static ScheduleDto ToDto(Schedule schedule) => new(
        schedule.Id.Value,
        schedule.TenantId,
        schedule.ScheduleType.ToString(),
        schedule.Expression.Value,
        schedule.TimeZone.Value,
        schedule.TaskType,
        schedule.TaskReferenceId,
        schedule.HolidayBehavior.ToString(),
        schedule.CalendarReference,
        schedule.Status.ToString(),
        schedule.CreatedAtUtc,
        schedule.LastTransitionAtUtc);

    public static ScheduleExecutionDto ToDto(ScheduleExecution execution) => new(
        execution.Id.Value,
        execution.ScheduleId.Value,
        execution.Status.ToString(),
        execution.JobIdentifier,
        execution.RetryCount,
        execution.DurationMs,
        execution.FailureReason,
        execution.TriggeredAtUtc,
        execution.CompletedAtUtc);
}
