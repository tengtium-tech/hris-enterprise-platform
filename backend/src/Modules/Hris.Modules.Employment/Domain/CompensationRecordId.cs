using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="CompensationRecord"/> child Entity of
/// <see cref="Employment"/>. Source: docs/04-modules/employment/domain/entities.md,
/// CompensationRecord Identity ("CompensationRecordId"). Added during the
/// `compensation` module's build -- see aggregates.md's and entities.md's own notes
/// on the Employment Aggregate's "owns" list, and <c>EMP-007</c>.
/// </summary>
public readonly record struct CompensationRecordId(Guid Value) : IStronglyTypedId;
