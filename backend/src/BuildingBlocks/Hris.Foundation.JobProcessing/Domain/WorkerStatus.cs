namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// A <see cref="Worker"/>'s own lifecycle -- the exact pair job-processing.md's own
/// Domain Events section names for it (<c>WorkerStarted</c>, <c>WorkerStopped</c>).
/// </summary>
public enum WorkerStatus
{
    Running = 0,
    Stopped = 1,
}
