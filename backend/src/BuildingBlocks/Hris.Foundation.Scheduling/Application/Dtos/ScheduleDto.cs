namespace Hris.Foundation.Scheduling.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetScheduleQuery</c> returns, per dto-design.md's own
/// convention.
/// </summary>
public sealed record ScheduleDto(
    Guid ScheduleId,
    Guid TenantId,
    string ScheduleType,
    string Expression,
    string TimeZone,
    string TaskType,
    string? TaskReferenceId,
    string HolidayBehavior,
    string? CalendarReference,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastTransitionAtUtc);
