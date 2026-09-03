using Hris.Foundation.Scheduling.Domain;

namespace Hris.Foundation.Scheduling.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static ScheduleExpression NewExpression(string? value = null) => ScheduleExpression.Create(value ?? "0 0 * * *").Value;

    public static ScheduleTimeZone NewTimeZone(string? value = null) => ScheduleTimeZone.Create(value ?? "Asia/Manila").Value;

    public static Schedule DraftSchedule(
        Guid? tenantId = null,
        ScheduleType scheduleType = ScheduleType.CronBased,
        ScheduleExpression? expression = null,
        ScheduleTimeZone? timeZone = null,
        string taskType = "PayrollProcessing",
        string? taskReferenceId = null,
        HolidayBehavior holidayBehavior = HolidayBehavior.ExecuteNormally,
        string? calendarReference = null,
        DateTimeOffset? nowUtc = null) =>
        Schedule.Create(
            tenantId ?? TenantId,
            scheduleType,
            expression ?? NewExpression(),
            timeZone ?? NewTimeZone(),
            taskType,
            taskReferenceId,
            holidayBehavior,
            calendarReference,
            nowUtc ?? NowUtc).Value;

    public static Schedule ValidatedSchedule(Guid? tenantId = null, DateTimeOffset? nowUtc = null)
    {
        var schedule = DraftSchedule(tenantId, nowUtc: nowUtc);
        schedule.Validate();
        return schedule;
    }

    public static Schedule ApprovedSchedule(Guid? tenantId = null, DateTimeOffset? nowUtc = null)
    {
        var schedule = ValidatedSchedule(tenantId, nowUtc);
        schedule.Approve();
        return schedule;
    }

    public static Schedule ActiveSchedule(Guid? tenantId = null, DateTimeOffset? nowUtc = null)
    {
        var schedule = ApprovedSchedule(tenantId, nowUtc);
        schedule.Activate(nowUtc ?? NowUtc);
        return schedule;
    }

    public static ScheduleExecution TriggeredExecution(
        ScheduleId? scheduleId = null, Guid? tenantId = null, string? jobIdentifier = "job-0001", int retryCount = 0, DateTimeOffset? nowUtc = null) =>
        ScheduleExecution.Trigger(
            scheduleId ?? new ScheduleId(Guid.NewGuid()), tenantId ?? TenantId, jobIdentifier, retryCount, nowUtc ?? NowUtc).Value;
}
