using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Identity of the <see cref="Worker"/> Aggregate Root -- one per registered worker
/// process instance, per job-processing.md's own Core Concepts ("Workers execute
/// queued jobs").
/// </summary>
public readonly record struct WorkerId(Guid Value) : IStronglyTypedId;
