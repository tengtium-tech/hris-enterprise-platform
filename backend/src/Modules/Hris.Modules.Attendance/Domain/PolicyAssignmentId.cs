using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>Strongly typed identifier for the <see cref="PolicyAssignment"/> child entity, unique within its owning <see cref="AttendancePolicy"/>.</summary>
public readonly record struct PolicyAssignmentId(Guid Value) : IStronglyTypedId;
