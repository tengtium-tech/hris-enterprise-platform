using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="SeparationRecord"/> child Entity of
/// <see cref="Employment"/>. Source: docs/04-modules/employment/domain/entities.md,
/// SeparationRecord Identity ("SeparationRecordId").
/// </summary>
public readonly record struct SeparationRecordId(Guid Value) : IStronglyTypedId;
