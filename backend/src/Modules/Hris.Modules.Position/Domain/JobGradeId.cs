using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Identity of the <see cref="JobGrade"/> Aggregate Root. Source:
/// docs/04-modules/position/domain/entities.md, Entity Identity ("JobGradeId").
/// </summary>
public readonly record struct JobGradeId(Guid Value) : IStronglyTypedId;
