namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// A <see cref="ScheduleExecution"/>'s own lifecycle -- the exact triad
/// scheduling-framework.md's own Domain Events section names for it
/// (<c>ScheduleTriggered</c>, <c>ScheduleCompleted</c>, <c>ScheduleFailed</c>), the
/// identical shape <c>SearchExecutionStatus</c> already establishes for its own
/// framework's request/response cycle.
/// </summary>
public enum ScheduleExecutionStatus
{
    Triggered = 0,
    Completed = 1,
    Failed = 2,
}
