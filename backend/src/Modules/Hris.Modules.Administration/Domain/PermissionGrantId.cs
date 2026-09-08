using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Identity of a <see cref="PermissionGrant"/> child entity, unique within its
/// owning <see cref="TenantRole"/>.
/// </summary>
public readonly record struct PermissionGrantId(Guid Value) : IStronglyTypedId;
