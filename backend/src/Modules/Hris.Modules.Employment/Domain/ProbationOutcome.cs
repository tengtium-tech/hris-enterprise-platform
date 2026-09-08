namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The evaluation outcome of a <see cref="ProbationRecord"/>. Source:
/// docs/04-modules/employment/domain/probation-confirmation.md's own "Probation
/// Evaluation" and "Confirmation (Regularization) Process" / "Probation Failure
/// Handling" sections.
/// </summary>
public enum ProbationOutcome
{
    /// <summary>Probation is in progress; no evaluation outcome recorded yet.</summary>
    Pending = 0,

    /// <summary>Probation completed successfully; the Employment was confirmed (regularized).</summary>
    Confirmed = 1,

    /// <summary>Probation ended without confirmation and no further extension was approved (PROB-005).</summary>
    Failed = 2,
}
