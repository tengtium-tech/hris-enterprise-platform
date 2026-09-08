using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Identity of the <see cref="Position"/> Aggregate Root. Source:
/// docs/04-modules/position/domain/entities.md, Entity Identity ("PositionId").
/// </summary>
public readonly record struct PositionId(Guid Value) : IStronglyTypedId;
