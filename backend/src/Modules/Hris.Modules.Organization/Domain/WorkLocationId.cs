using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of the <see cref="WorkLocation"/> Aggregate Root. Source:
/// docs/04-modules/organization/domain/entities.md, WorkLocation (Aggregate Root),
/// Identity: WorkLocationId.
/// </summary>
public readonly record struct WorkLocationId(Guid Value) : IStronglyTypedId;
