using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for the <see cref="LeaveAdjustment"/> aggregate root.</summary>
public readonly record struct LeaveAdjustmentId(Guid Value) : IStronglyTypedId;
