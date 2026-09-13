using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for the <see cref="LeaveType"/> aggregate root.</summary>
public readonly record struct LeaveTypeId(Guid Value) : IStronglyTypedId;
