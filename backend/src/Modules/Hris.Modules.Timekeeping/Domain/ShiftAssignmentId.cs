using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Identity of the <see cref="ShiftAssignment"/> Aggregate Root.
/// </summary>
public readonly record struct ShiftAssignmentId(Guid Value) : IStronglyTypedId;
