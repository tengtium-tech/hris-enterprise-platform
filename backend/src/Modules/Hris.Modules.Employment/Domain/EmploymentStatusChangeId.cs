using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="EmploymentStatusChange"/> child Entity of
/// <see cref="Employment"/>. Source: docs/04-modules/employment/domain/entities.md,
/// EmploymentStatusChange Identity ("EmploymentStatusChangeId").
/// </summary>
public readonly record struct EmploymentStatusChangeId(Guid Value) : IStronglyTypedId;
