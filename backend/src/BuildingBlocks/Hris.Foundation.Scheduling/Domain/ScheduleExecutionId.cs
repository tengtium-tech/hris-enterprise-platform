using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// Identity of the <see cref="ScheduleExecution"/> Aggregate Root -- one per actual
/// trigger/fire occurrence. See <see cref="ScheduleExecution"/>'s own remarks for why
/// this is a separate, population-scale Aggregate Root from <see cref="Schedule"/>, not
/// a child Entity of it.
/// </summary>
public readonly record struct ScheduleExecutionId(Guid Value) : IStronglyTypedId;
