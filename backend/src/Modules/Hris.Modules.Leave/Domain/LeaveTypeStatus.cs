namespace Hris.Modules.Leave.Domain;

/// <summary>
/// A statutory (Platform-scope) <see cref="LeaveType"/> is never available in
/// <see cref="Inactive"/> (LV-004); only a tenant-defined type can be deactivated.
/// </summary>
public enum LeaveTypeStatus
{
    Active,
    Inactive,
}
