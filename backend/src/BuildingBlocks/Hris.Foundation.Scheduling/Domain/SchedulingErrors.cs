using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class SchedulingErrors
{
    public static readonly Error ScheduleExpressionRequired = new(
        "Scheduling.ScheduleExpressionRequired",
        "A schedule expression is required.",
        ErrorCategory.Validation);

    public static readonly Error ScheduleExpressionTooLong = new(
        "Scheduling.ScheduleExpressionTooLong",
        "A schedule expression cannot exceed 500 characters.",
        ErrorCategory.Validation);

    public static readonly Error TimeZoneRequired = new(
        "Scheduling.TimeZoneRequired",
        "A time zone is required.",
        ErrorCategory.Validation);

    public static readonly Error TimeZoneTooLong = new(
        "Scheduling.TimeZoneTooLong",
        "A time zone identifier cannot exceed 100 characters.",
        ErrorCategory.Validation);

    public static readonly Error TaskTypeRequired = new(
        "Scheduling.TaskTypeRequired",
        "A task type is required.",
        ErrorCategory.Validation);

    public static readonly Error ScheduleNotFound = new(
        "Scheduling.ScheduleNotFound",
        "No schedule exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidScheduleLifecycleTransition = new(
        "Scheduling.InvalidScheduleLifecycleTransition",
        "This transition is not valid from the schedule's current status.",
        ErrorCategory.Domain);

    public static readonly Error ScheduleExecutionNotFound = new(
        "Scheduling.ScheduleExecutionNotFound",
        "No schedule execution exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidScheduleExecutionTransition = new(
        "Scheduling.InvalidScheduleExecutionTransition",
        "This transition is not valid from the schedule execution's current status.",
        ErrorCategory.Domain);

    public static readonly Error FailureReasonRequired = new(
        "Scheduling.FailureReasonRequired",
        "A reason is required to fail a schedule execution.",
        ErrorCategory.Validation);

    public static readonly Error RetryCountNegative = new(
        "Scheduling.RetryCountNegative",
        "A retry count cannot be negative.",
        ErrorCategory.Validation);
}
