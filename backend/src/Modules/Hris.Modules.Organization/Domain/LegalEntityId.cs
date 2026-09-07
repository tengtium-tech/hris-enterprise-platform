using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of the <see cref="LegalEntity"/> Aggregate Root. Source:
/// docs/04-modules/organization/domain/entities.md, LegalEntity (Aggregate Root),
/// Identity: LegalEntityId.
/// </summary>
public readonly record struct LegalEntityId(Guid Value) : IStronglyTypedId;
