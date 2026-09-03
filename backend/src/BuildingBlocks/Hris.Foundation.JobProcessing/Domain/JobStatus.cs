namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// job-processing.md's own Job Lifecycle diagram: the main line "Submitted -&gt; Queued
/// -&gt; Scheduled -&gt; Running -&gt; Completed," plus its own alternative outcomes "Failed
/// -&gt; Retry -&gt; Completed" (retry is a transition back to <see cref="Queued"/>, not its
/// own persisted status) and "Failed -&gt; Dead Letter Queue." <see cref="Cancelled"/> is
/// added beyond the diagram's own three named branches -- the Dead Letter Queue
/// section's own "Permanent Cancellation" bullet names it as a real outcome the
/// diagram itself does not draw.
/// </summary>
public enum JobStatus
{
    Submitted = 0,
    Queued = 1,
    Scheduled = 2,
    Running = 3,
    Completed = 4,
    Failed = 5,
    DeadLetter = 6,
    Cancelled = 7,
}
