namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// A tenant's product edition, per business-model.md (DOC-011) Section 3's four
/// editions -- Starter (self-service), Growth, Enterprise, and Government (built on
/// Enterprise, with sector-specific requirements). Persisted directly on
/// <see cref="Tenant"/> itself, per the Tenant Aggregate/Owns section: "a persisted,
/// queryable field on Tenant itself, not a derived value."
/// </summary>
public enum SubscriptionPlan
{
    Starter = 0,
    Growth = 1,
    Enterprise = 2,
    Government = 3,
}
