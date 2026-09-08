namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The legal artifact's own progression, entirely separate from
/// <see cref="EmploymentLifecycleStage"/> -- one Employment may run through several
/// successive Contracts (ADR-0008, "Why the Contract Is Not the Relationship").
/// Source: docs/04-modules/employment/domain/employment-contracts.md's own "Contract
/// Lifecycle" diagram and ADR-0008's own Contract Lifecycle table.
/// </summary>
public enum ContractLifecycleStage
{
    /// <summary>Terms being prepared.</summary>
    Draft = 0,

    /// <summary>Signed or authorized, but not yet in force. Distinct from <see cref="Effective"/> because a contract is routinely signed before its start date.</summary>
    Approved = 1,

    /// <summary>In force.</summary>
    Effective = 2,

    /// <summary>Reached its natural end date without renewal.</summary>
    Expired = 3,

    /// <summary>Replaced by a renewal or conversion contract before reaching its own end date (for example, Probationary superseded by Regular on regularization).</summary>
    Superseded = 4,

    /// <summary>Ended early, after having taken effect.</summary>
    Closed = 5,

    /// <summary>Ended before ever taking effect (for example, a withdrawn offer). Distinct from <see cref="Closed"/> because no work was performed under it.</summary>
    Cancelled = 6,
}
