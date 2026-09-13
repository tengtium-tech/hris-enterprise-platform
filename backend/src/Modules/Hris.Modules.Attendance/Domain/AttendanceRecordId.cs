using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>Strongly typed identifier for the <see cref="AttendanceRecord"/> aggregate root.</summary>
public readonly record struct AttendanceRecordId(Guid Value) : IStronglyTypedId;
