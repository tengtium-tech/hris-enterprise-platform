namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// Seven persisted values, per tenant-framework.md's Tenant Aggregate/State Machine
/// section, which corrects the Tenant Lifecycle diagram's own misleading eight-state
/// reading: "Reactivated is a transition name, not a state a tenant rests in." A
/// Suspended tenant that reactivates re-enters <see cref="Active"/> directly -- there
/// is no <c>Reactivated</c> member here to rest in. See <see cref="Tenant"/>'s own
/// state-machine remarks for the full transition table this enum's values are drawn
/// from.
/// </summary>
public enum TenantLifecycleState
{
    Requested = 0,
    Provisioning = 1,
    Configured = 2,
    Active = 3,
    Suspended = 4,
    Archived = 5,
    Deleted = 6,
}
