using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>Strongly typed identifier for the <see cref="BiometricEnrollment"/> aggregate root.</summary>
public readonly record struct BiometricEnrollmentId(Guid Value) : IStronglyTypedId;
