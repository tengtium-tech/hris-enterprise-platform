namespace Hris.SharedKernel;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/error-pattern.md's Error
/// Categories section, which names exactly these six expected-failure categories
/// (five through Sprint 5; <see cref="Entitlement"/> added with the Entitlement &amp;
/// Process Pack Framework in Sprint 6 -- that document's own "Entitlement Errors"
/// subsection states why it must never share <see cref="Authorization"/>: the two
/// answer different questions ("may this user do this" vs. "has this tenant
/// activated this"), and CTR-ENT-007 requires the two remain distinguishable).
/// <see cref="None"/> is not one of the document's categories -- it exists only as
/// the sentinel <see cref="Error.None"/> carries so a successful <see cref="Result"/>
/// has a well-defined, never-inspected <see cref="Error"/> value.
/// </summary>
public enum ErrorCategory
{
    None = 0,
    Domain,
    Validation,
    Entitlement,
    Authorization,
    Conflict,
    NotFound,
}
