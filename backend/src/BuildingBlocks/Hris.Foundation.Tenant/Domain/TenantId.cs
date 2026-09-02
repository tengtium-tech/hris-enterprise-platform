using Hris.SharedKernel;

namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// Identity of the <see cref="Tenant"/> Aggregate Root, per tenant-framework.md's own
/// Tenant Aggregate/Root section: "assigned once, at Requested, and never
/// reassigned -- every other aggregate's own TenantId foreign reference depends on
/// this holding." Every one of the nineteen business modules, plus
/// TenantConfiguration and IntegrationCredential, references this type.
/// </summary>
public readonly record struct TenantId(Guid Value) : IStronglyTypedId;
