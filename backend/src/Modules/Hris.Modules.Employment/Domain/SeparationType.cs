namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The circumstance under which an Employment reached the terminal
/// <see cref="EmploymentLifecycleStage.Separated"/> stage -- a reason carried by that
/// stage, never a status of its own (STAT-004, ADR-0008's own "Separation reasons
/// remain reasons, not states"). Source:
/// docs/04-modules/employment/domain/value-objects.md's SeparationType and
/// business-rules.md's SEP-002, reconciled: business-rules.md's own five values
/// (Resigned, Terminated, EndOfContract, Retired, Deceased) plus value-objects.md's
/// additional sixth value (Redundancy, listed there but not in SEP-002's own list) --
/// included since a distinct reason does no harm and value-objects.md names it
/// explicitly as a Separation Type value.
/// </summary>
public enum SeparationType
{
    /// <summary>Voluntary resignation.</summary>
    Resigned = 0,

    /// <summary>Involuntary termination.</summary>
    Terminated = 1,

    /// <summary>Fixed-term or project-based agreement expired without renewal. Only fixed-term Employment Types may separate this way (STAT-item in employment-status.md, CON-006).</summary>
    EndOfContract = 2,

    /// <summary>Retirement.</summary>
    Retired = 3,

    /// <summary>Employment ended on the employee's death. Also recorded as an Employee Lifecycle Stage change that propagates to every Employment (ADR-0008); this module records only this Employment's own separation.</summary>
    Deceased = 4,

    /// <summary>Position eliminated for business reasons.</summary>
    Redundancy = 5,
}
