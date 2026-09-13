using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>Strongly typed identifier for the <see cref="AttendancePolicy"/> aggregate root.</summary>
public readonly record struct AttendancePolicyId(Guid Value) : IStronglyTypedId;
