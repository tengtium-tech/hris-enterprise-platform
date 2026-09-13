using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for the <see cref="LeavePolicy"/> aggregate root.</summary>
public readonly record struct LeavePolicyId(Guid Value) : IStronglyTypedId;
