using Hris.SharedKernel;

namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// Identifies the actor behind every Platform-Operator-Facing command except
/// <c>ActivateTenantCommand</c> (docs/03-foundation/tenant-framework.md's own
/// Platform-Operator-Facing Commands and Queries section). Deliberately not
/// <c>Hris.Foundation.Identity.Domain.UserAccountId</c>: ADR-0009
/// (docs/15-adr/0009-platform-operations-boundary.md) decided a Platform Operator
/// account is "never associated with any TenantId... a different account universe,"
/// so reusing the tenant-scoped identity type here would silently reintroduce the
/// exact coupling that ADR exists to prevent.
///
/// No framework in IMPLEMENTATION-PLAN.md (Sprint 3's kernel or Sprint 4's own eight)
/// builds Platform Operator authentication or account management -- that capability is
/// undescribed beyond ADR-0009 and platform-operations-roles.md naming the role. This
/// type is deliberately the minimal shape Tenant Framework needs to carry "who did
/// this" on its own events and commands, per CTR-ARC-002's "reference by strongly
/// typed identifier," without inventing an identity system this framework does not
/// own. When a real Platform Operator identity concept is built, this id should either
/// be superseded by it or kept as an alias -- not duplicated ad hoc a second time.
/// </summary>
public readonly record struct PlatformOperatorId(Guid Value) : IStronglyTypedId;
