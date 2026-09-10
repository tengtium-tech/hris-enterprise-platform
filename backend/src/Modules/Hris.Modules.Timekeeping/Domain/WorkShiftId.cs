using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Identity of the <see cref="WorkShift"/> Aggregate Root.
/// </summary>
public readonly record struct WorkShiftId(Guid Value) : IStronglyTypedId;
