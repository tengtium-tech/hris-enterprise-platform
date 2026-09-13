namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Who owns a <see cref="LeaveType"/>'s core identity. A Platform-scope type is
/// seeded centrally and read-only to every tenant; a Tenant-scope type is fully owned
/// and editable by the tenant that created it. Source:
/// docs/04-modules/leave/domain/leave-types.md.
/// </summary>
public enum LeaveTypeScope
{
    Platform,
    Tenant,
}
