using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Identity of the <see cref="Job"/> Aggregate Root -- one per submitted unit of
/// background work. See <see cref="Job"/>'s own remarks for why this is a separate,
/// population-scale Aggregate Root from <see cref="JobQueue"/>, not a child Entity of
/// it.
/// </summary>
public readonly record struct JobId(Guid Value) : IStronglyTypedId;
