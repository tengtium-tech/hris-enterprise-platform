using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="ReportingAssignment"/> child Entity of
/// <see cref="EmploymentAssignment"/>. Source:
/// docs/04-modules/employment/domain/entities.md, ReportingAssignment Identity
/// ("ReportingAssignmentId"). Tracks the reporting-manager timeline independently of
/// <see cref="AssignmentHistoryRecord"/>'s position/organizational-unit timeline,
/// since a reporting line may change without a position change (matrixed or
/// project-based reporting) -- employment-assignments.md's own "Reporting
/// Relationship" section.
/// </summary>
public readonly record struct ReportingAssignmentId(Guid Value) : IStronglyTypedId;
