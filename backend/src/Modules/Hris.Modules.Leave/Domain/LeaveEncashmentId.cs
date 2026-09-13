using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for the <see cref="LeaveEncashment"/> aggregate root.</summary>
public readonly record struct LeaveEncashmentId(Guid Value) : IStronglyTypedId;
