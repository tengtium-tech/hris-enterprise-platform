using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Identity of the <see cref="JobClassification"/> Aggregate Root. Source:
/// docs/04-modules/position/domain/entities.md, Entity Identity ("JobClassificationId").
/// </summary>
public readonly record struct JobClassificationId(Guid Value) : IStronglyTypedId;
