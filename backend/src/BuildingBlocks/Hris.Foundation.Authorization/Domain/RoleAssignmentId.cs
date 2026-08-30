using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>Identity of the <see cref="RoleAssignment"/> Aggregate Root.</summary>
public readonly record struct RoleAssignmentId(Guid Value) : IStronglyTypedId;
