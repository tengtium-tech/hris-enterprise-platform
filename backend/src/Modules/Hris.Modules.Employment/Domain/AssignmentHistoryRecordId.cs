using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="AssignmentHistoryRecord"/> child Entity of
/// <see cref="EmploymentAssignment"/>. Source:
/// docs/04-modules/employment/domain/entities.md, AssignmentHistoryRecord Identity
/// ("AssignmentHistoryRecordId").
/// </summary>
public readonly record struct AssignmentHistoryRecordId(Guid Value) : IStronglyTypedId;
