using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of a <see cref="CostCenter"/> child entity of the
/// <see cref="Organization"/> Aggregate. Source:
/// docs/04-modules/organization/domain/entities.md, CostCenter, Identity:
/// CostCenterId.
/// </summary>
public readonly record struct CostCenterId(Guid Value) : IStronglyTypedId;
