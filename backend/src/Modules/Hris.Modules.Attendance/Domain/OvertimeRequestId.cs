using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>Strongly typed identifier for the <see cref="OvertimeRequest"/> aggregate root.</summary>
public readonly record struct OvertimeRequestId(Guid Value) : IStronglyTypedId;
