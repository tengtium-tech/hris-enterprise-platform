using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>Strongly typed identifier for a <see cref="PolicyAssignment"/> entity.</summary>
public readonly record struct PolicyAssignmentId(Guid Value) : IStronglyTypedId;
