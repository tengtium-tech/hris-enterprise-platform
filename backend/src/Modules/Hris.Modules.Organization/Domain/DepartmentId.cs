using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of a <see cref="Department"/> child entity of the
/// <see cref="Organization"/> Aggregate. Source:
/// docs/04-modules/organization/domain/entities.md, Department, Identity:
/// DepartmentId.
/// </summary>
public readonly record struct DepartmentId(Guid Value) : IStronglyTypedId;
