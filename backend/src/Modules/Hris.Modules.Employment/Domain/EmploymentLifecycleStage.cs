namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The employment relationship's own progression, independent of Employment Type
/// and Operational Status. Source:
/// docs/04-modules/employment/domain/employment-status.md's "Employment Lifecycle"
/// table and ADR-0008. <see cref="Separated"/> is terminal and carries a separation
/// reason (<see cref="SeparationType"/>) rather than being split into per-reason
/// stages -- STAT-004, business-rules.md.
/// </summary>
public enum EmploymentLifecycleStage
{
    /// <summary>Created but not yet operative. Requires a Contract and an Assignment before activation.</summary>
    Draft = 0,

    /// <summary>The agreement is in force.</summary>
    Active = 1,

    /// <summary>The agreement has ended. Terminal; never returns to <see cref="Active"/> (STAT-003, EMP-006).</summary>
    Separated = 2,
}
