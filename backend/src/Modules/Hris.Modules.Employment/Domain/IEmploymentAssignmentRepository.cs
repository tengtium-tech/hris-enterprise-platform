namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Repository contract for the <see cref="EmploymentAssignment"/> Aggregate Root,
/// per repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split.
///
/// <see cref="HasValidAssignmentAsync"/> exists so the Application-layer command
/// handler can compute <see cref="Employment.Activate"/>'s own
/// <c>hasValidAssignment</c> boolean (EMP-003). <see cref="WouldCreateCircularReportingAsync"/>
/// exists because <see cref="EmploymentAssignment.ReportingManagerEmploymentId"/>
/// references another Employment's own current Assignment: detecting a circular
/// reporting chain requires walking the candidate manager's own reporting line, a
/// graph traversal only the Infrastructure layer can perform (ASG-006) -- the
/// identical reasoning <c>IPositionRepository.WouldCreateCircularReportingAsync</c>
/// already establishes for Position's own self-referencing hierarchy.
/// </summary>
public interface IEmploymentAssignmentRepository
{
    Task<EmploymentAssignment?> GetByIdAsync(EmploymentAssignmentId id, CancellationToken cancellationToken);

    Task<EmploymentAssignment?> GetCurrentByEmploymentIdAsync(
        Guid tenantId, Guid employmentId, CancellationToken cancellationToken);

    Task<bool> HasValidAssignmentAsync(Guid tenantId, Guid employmentId, CancellationToken cancellationToken);

    Task<bool> WouldCreateCircularReportingAsync(
        Guid tenantId, Guid employmentId, Guid candidateReportingManagerEmploymentId, CancellationToken cancellationToken);

    Task AddAsync(EmploymentAssignment assignment, CancellationToken cancellationToken);
}
