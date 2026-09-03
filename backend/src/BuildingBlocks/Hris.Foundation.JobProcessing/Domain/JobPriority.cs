namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// job-processing.md's own Core Concepts: "Example priorities: Critical, High, Normal,
/// Low, Background. Higher priority jobs should be processed first." Ordinal order
/// matches processing priority (lower value processed first) -- actually enforcing that
/// ordering at dequeue time is a real worker/queue-execution concern outside this
/// Sprint's own Scope, see <c>DependencyInjection.cs</c>'s own remarks.
/// </summary>
public enum JobPriority
{
    Critical = 0,
    High = 1,
    Normal = 2,
    Low = 3,
    Background = 4,
}
