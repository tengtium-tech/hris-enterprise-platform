using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>Strongly typed identifier for the <see cref="AttendanceAdjustment"/> aggregate root.</summary>
public readonly record struct AttendanceAdjustmentId(Guid Value) : IStronglyTypedId;
