namespace Hris.Modules.Leave.Domain;

/// <summary>
/// What kind of leave a <see cref="LeaveType"/> is, as distinct from
/// <see cref="LeaveTypeScope"/> (who owns it). Source:
/// docs/04-modules/leave/domain/leave-types.md.
/// </summary>
public enum LeaveTypeCategory
{
    Statutory,
    CompanyDefined,
}
