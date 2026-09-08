using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Identity of a <see cref="RoleAssignment"/> child entity, unique within its
/// owning <see cref="UserAccount"/>.
/// </summary>
public readonly record struct RoleAssignmentId(Guid Value) : IStronglyTypedId;
