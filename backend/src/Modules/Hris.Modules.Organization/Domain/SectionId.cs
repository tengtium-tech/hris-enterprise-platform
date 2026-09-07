using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of a <see cref="Section"/> child entity of the <see cref="Organization"/>
/// Aggregate. Source: docs/04-modules/organization/domain/entities.md, Section,
/// Identity: SectionId.
/// </summary>
public readonly record struct SectionId(Guid Value) : IStronglyTypedId;
