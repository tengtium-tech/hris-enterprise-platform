using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Identity of the <see cref="JobFamily"/> Aggregate Root. Source:
/// docs/04-modules/position/domain/entities.md, Entity Identity ("JobFamilyId").
/// </summary>
public readonly record struct JobFamilyId(Guid Value) : IStronglyTypedId;
