namespace Hris.Foundation.Entitlement.Domain;

/// <summary>
/// This framework's own mirror of <c>Hris.Foundation.Tenant.Domain.SubscriptionPlan</c>,
/// deliberately re-declared rather than referenced -- entitlement-framework.md's own
/// Dependencies section states why: the same "explicit reference, no concrete
/// cross-framework dependency" pattern workflow-engine.md's own closed participant-role
/// vocabulary already establishes, applied here so this framework's own assembly is
/// not coupled to Tenant Framework's. A caller that already holds a
/// <c>SubscriptionPlan</c> value translates it to this type at the call site; the two
/// enums share the same four member names by design, so that translation is a plain
/// name-to-name mapping, never a lookup table of its own.
/// </summary>
public enum TenantEditionCode
{
    Starter = 0,
    Growth = 1,
    Enterprise = 2,
    Government = 3,
}
