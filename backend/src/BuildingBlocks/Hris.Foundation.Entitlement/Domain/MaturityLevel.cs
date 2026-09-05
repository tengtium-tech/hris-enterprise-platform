namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// How elaborate a Process Pack's processes are, per DOC-014 Section 3.2, which names
/// these three levels "1, 2, 3." Numbered here from zero instead (CA1008 requires an
/// enum define a zero-valued member, and DOC-014 itself has no "Level 0" concept to
/// spend a real member naming) -- the same "purpose-fit enum, ordinal order carries
/// the meaning" precedent <c>OrganizationalScopeLevel</c> already establishes for its
/// own similarly one-based source document. Ordinal order is what
/// <see cref="EntitlementEvaluator"/> actually compares (<c>&gt;=</c>, since levels
/// are cumulative), never the raw numeric value against DOC-014's own "1/2/3" text.
/// </summary>
public enum MaturityLevel
{
    Essential = 0,
    Standard = 1,
    Advanced = 2,
}
