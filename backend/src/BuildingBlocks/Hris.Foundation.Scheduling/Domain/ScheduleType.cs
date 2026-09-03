namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// scheduling-framework.md's own Scheduling Types section: One-Time, Recurring,
/// Cron-Based, Delayed, Calendar-Based.
/// </summary>
public enum ScheduleType
{
    OneTime = 0,
    Recurring = 1,
    CronBased = 2,
    Delayed = 3,
    CalendarBased = 4,
}
