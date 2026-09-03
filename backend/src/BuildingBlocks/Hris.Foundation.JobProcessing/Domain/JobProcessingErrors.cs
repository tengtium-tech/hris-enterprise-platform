using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class JobProcessingErrors
{
    public static readonly Error JobQueueNameRequired = new(
        "JobProcessing.JobQueueNameRequired",
        "A job queue name is required.",
        ErrorCategory.Validation);

    public static readonly Error JobQueueNameTooLong = new(
        "JobProcessing.JobQueueNameTooLong",
        "A job queue name cannot exceed 200 characters.",
        ErrorCategory.Validation);

    public static readonly Error MaxConcurrencyOutOfRange = new(
        "JobProcessing.MaxConcurrencyOutOfRange",
        "Maximum concurrency must be at least 1.",
        ErrorCategory.Validation);

    public static readonly Error DefaultMaxRetriesNegative = new(
        "JobProcessing.DefaultMaxRetriesNegative",
        "Default maximum retries cannot be negative.",
        ErrorCategory.Validation);

    public static readonly Error DefaultRetryDelayNegative = new(
        "JobProcessing.DefaultRetryDelayNegative",
        "Default retry delay cannot be negative.",
        ErrorCategory.Validation);

    public static readonly Error JobQueueNotFound = new(
        "JobProcessing.JobQueueNotFound",
        "No job queue exists for the given identifier or name.",
        ErrorCategory.NotFound);

    public static readonly Error JobQueueNameAlreadyRegistered = new(
        "JobProcessing.JobQueueNameAlreadyRegistered",
        "A job queue is already registered under this name.",
        ErrorCategory.Conflict);

    public static readonly Error JobTypeRequired = new(
        "JobProcessing.JobTypeRequired",
        "A job type is required.",
        ErrorCategory.Validation);

    public static readonly Error JobNotFound = new(
        "JobProcessing.JobNotFound",
        "No job exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidJobLifecycleTransition = new(
        "JobProcessing.InvalidJobLifecycleTransition",
        "This transition is not valid from the job's current status.",
        ErrorCategory.Domain);

    public static readonly Error RetryLimitExceeded = new(
        "JobProcessing.RetryLimitExceeded",
        "This job has already reached its own maximum retry count; move it to the dead letter queue instead.",
        ErrorCategory.Domain);

    public static readonly Error FailureReasonRequired = new(
        "JobProcessing.FailureReasonRequired",
        "A reason is required to fail a job.",
        ErrorCategory.Validation);

    public static readonly Error WorkerInstanceIdRequired = new(
        "JobProcessing.WorkerInstanceIdRequired",
        "A worker instance id is required.",
        ErrorCategory.Validation);

    public static readonly Error WorkerNotFound = new(
        "JobProcessing.WorkerNotFound",
        "No worker exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidWorkerLifecycleTransition = new(
        "JobProcessing.InvalidWorkerLifecycleTransition",
        "This transition is not valid from the worker's current status.",
        ErrorCategory.Domain);
}
