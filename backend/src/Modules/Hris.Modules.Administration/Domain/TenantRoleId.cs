using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Identity of the <see cref="TenantRole"/> Aggregate Root.
/// </summary>
public readonly record struct TenantRoleId(Guid Value) : IStronglyTypedId;
