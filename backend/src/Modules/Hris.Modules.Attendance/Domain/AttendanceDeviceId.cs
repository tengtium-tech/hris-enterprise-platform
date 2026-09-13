using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>Strongly typed identifier for the <see cref="AttendanceDevice"/> aggregate root.</summary>
public readonly record struct AttendanceDeviceId(Guid Value) : IStronglyTypedId;
