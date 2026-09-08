using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="ProbationRecord"/> child Entity of
/// <see cref="Employment"/>. Source: docs/04-modules/employment/domain/entities.md,
/// ProbationRecord Identity ("ProbationRecordId").
/// </summary>
public readonly record struct ProbationRecordId(Guid Value) : IStronglyTypedId;
