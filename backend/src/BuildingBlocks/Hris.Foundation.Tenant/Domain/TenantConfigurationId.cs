using Hris.SharedKernel;

namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// Forward reference to `administration`'s own <c>TenantConfiguration</c> Aggregate
/// Root (docs/04-modules/administration/domain/tenant-configuration.md), which does
/// not exist in code yet -- `administration` is a Phase 2 business module and this
/// Sprint (Sprint 4) builds Foundation frameworks only, per IMPLEMENTATION-PLAN.md.
///
/// This type exists solely because tenant-framework.md's own Domain Events section
/// gives <c>TenantProvisioned</c> an exact, already-decided field list --
/// <c>TenantId, TenantConfigurationId</c> -- and <see cref="Tenant.CompleteProvisioning"/>
/// needs a real parameter type to require proof one was created before the aggregate
/// can leave <c>Provisioning</c> (Tenant Aggregate, Invariants: "A Tenant cannot reach
/// Active without a TenantConfiguration already existing for it"). It is not an
/// invitation to build TenantConfiguration itself here -- that remains
/// `administration`'s own aggregate, referenced only by id (CTR-ARC-002), the same
/// "Does Not Own" boundary the Tenant Aggregate section states explicitly. When
/// `administration` is built, its own <c>TenantConfigurationId</c> should be
/// Guid-backed the same way every strongly typed id in this platform is
/// (strongly-typed-ids.md), making the two structurally identical; this one should
/// then be retired in favor of a real reference, not kept as a second definition.
/// </summary>
public readonly record struct TenantConfigurationId(Guid Value) : IStronglyTypedId;
