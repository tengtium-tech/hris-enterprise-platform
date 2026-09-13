using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for the <see cref="LeaveBalance"/> aggregate root.</summary>
public readonly record struct LeaveBalanceId(Guid Value) : IStronglyTypedId;
