namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// scheduling-framework.md's own Holiday Awareness section: "Schedules may: Execute
/// Normally, Skip Holidays, Move to Previous Business Day, Move to Next Business Day,
/// Pause During Company Shutdown. Holiday behavior should be configurable." This Sprint
/// records the configured behavior only -- evaluating it against a real holiday
/// calendar at trigger time is Job Processing Framework's own concern, per
/// scheduling-framework.md's own Scope exclusion ("Background Job Execution").
/// </summary>
public enum HolidayBehavior
{
    ExecuteNormally = 0,
    SkipHolidays = 1,
    MoveToPreviousBusinessDay = 2,
    MoveToNextBusinessDay = 3,
    PauseDuringShutdown = 4,
}
