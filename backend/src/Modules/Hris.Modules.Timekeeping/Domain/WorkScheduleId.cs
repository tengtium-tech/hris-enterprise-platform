using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Identity of the <see cref="WorkSchedule"/> Aggregate Root.
/// </summary>
public readonly record struct WorkScheduleId(Guid Value) : IStronglyTypedId;
