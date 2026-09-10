using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Identity of a <see cref="ScheduleAssignment"/>, unique within its
/// <see cref="WorkSchedule"/>. Reached only through that root; no repository exists
/// for it (CTR-ARC-004).
/// </summary>
public readonly record struct ScheduleAssignmentId(Guid Value) : IStronglyTypedId;
