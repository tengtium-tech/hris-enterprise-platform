using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Identity of the <see cref="JobQueue"/> Aggregate Root, per job-processing.md's own
/// Core Concepts ("Queues organize jobs awaiting execution... may have independent
/// processing policies").
/// </summary>
public readonly record struct JobQueueId(Guid Value) : IStronglyTypedId;
