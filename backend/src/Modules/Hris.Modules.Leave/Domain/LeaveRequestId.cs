using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for the <see cref="LeaveRequest"/> aggregate root.</summary>
public readonly record struct LeaveRequestId(Guid Value) : IStronglyTypedId;
