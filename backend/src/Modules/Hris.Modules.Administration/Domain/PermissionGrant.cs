using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// One permission composed into a <see cref="TenantRole"/>. Source:
/// docs/04-modules/administration/domain/entities.md's PermissionGrant. A child
/// Entity of <see cref="TenantRole"/>, never an Aggregate Root of its own -- there
/// is no <c>PermissionGrantRepository</c> (`CTR-ARC-004`). Its constructor is
/// <c>internal</c>.
/// </summary>
public sealed class PermissionGrant : Entity<PermissionGrantId>
{
    public PermissionReference Permission { get; }

    public Guid AddedBy { get; }

    public DateTimeOffset AddedOn { get; }

    internal PermissionGrant(PermissionGrantId id, PermissionReference permission, Guid addedBy, DateTimeOffset addedOn)
        : base(id)
    {
        Permission = permission;
        AddedBy = addedBy;
        AddedOn = addedOn;
    }
}
