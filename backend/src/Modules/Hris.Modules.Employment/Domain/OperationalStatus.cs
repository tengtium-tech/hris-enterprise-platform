namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Whether work is currently being performed under an Employment, independent of
/// Employment Type and Employment Lifecycle stage. Source:
/// docs/04-modules/employment/domain/value-objects.md, OperationalStatus, and
/// employment-status.md's own "Operational Status" table. Owned by Employment, never
/// by Employee -- a person may hold two Employments with differing availability
/// (ADR-0008, "Why Operational Status Belongs to Employment").
/// </summary>
public enum OperationalStatus
{
    /// <summary>Work is being performed normally.</summary>
    Active = 0,

    /// <summary>Approved leave; the agreement continues.</summary>
    OnLeave = 1,

    /// <summary>Work privileges restricted, typically pending a disciplinary process. Does not end the Employment (STAT-005).</summary>
    Suspended = 2,

    /// <summary>Working under this agreement but placed elsewhere.</summary>
    Seconded = 3,
}
