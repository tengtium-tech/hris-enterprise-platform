using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Identity of a <see cref="Team"/> child entity of the <see cref="Organization"/>
/// Aggregate. Source: docs/04-modules/organization/domain/entities.md, Team,
/// Identity: TeamId.
/// </summary>
public readonly record struct TeamId(Guid Value) : IStronglyTypedId;
