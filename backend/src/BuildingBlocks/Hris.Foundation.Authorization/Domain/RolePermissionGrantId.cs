using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>Identity of the <see cref="RolePermissionGrant"/> Aggregate Root.</summary>
public readonly record struct RolePermissionGrantId(Guid Value) : IStronglyTypedId;
