namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The unit a <see cref="CompensationAmount"/> is expressed against. Source:
/// docs/04-modules/employment/domain/employment-categories.md's own "Compensation
/// Basis" dimension (5 values, including Commission) and entities.md's
/// CompensationRecord description (4 values, omitting Commission) -- reconciled by
/// taking the fuller 5-value set, since employment-categories.md is the document
/// that actually defines this dimension and entities.md's own list reads as
/// illustrative rather than exhaustive.
/// </summary>
public enum CompensationBasis
{
    Monthly = 0,
    Daily = 1,
    Hourly = 2,
    Commission = 3,
    PieceRate = 4,
}
