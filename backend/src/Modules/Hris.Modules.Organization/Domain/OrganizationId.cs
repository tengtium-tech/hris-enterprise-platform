using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of the <see cref="Organization"/> Aggregate Root. Source:
/// docs/04-modules/organization/domain/entities.md, Organization (Aggregate Root),
/// Identity: OrganizationId.
/// </summary>
public readonly record struct OrganizationId(Guid Value) : IStronglyTypedId;
