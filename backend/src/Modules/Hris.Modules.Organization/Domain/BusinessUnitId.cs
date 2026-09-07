using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of a <see cref="BusinessUnit"/> child entity of the
/// <see cref="Organization"/> Aggregate. Source:
/// docs/04-modules/organization/domain/entities.md, BusinessUnit, Identity:
/// BusinessUnitId.
/// </summary>
public readonly record struct BusinessUnitId(Guid Value) : IStronglyTypedId;
