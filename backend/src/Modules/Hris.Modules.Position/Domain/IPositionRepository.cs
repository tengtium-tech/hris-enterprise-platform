namespace Hris.Modules.Position.Domain;

/// <summary>
/// Repository contract for the <see cref="Position"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split. <see cref="GetByIdAsync"/> deliberately does not take a
/// tenant identifier, the same established pattern <c>IOrganizationRepository</c>
/// already follows: the calling Application-layer handler verifies the loaded
/// aggregate's own <see cref="Position.TenantId"/>.
///
/// <see cref="ExistsWithNumberAsync"/> exists because position-numbering.md's own
/// tenant-wide Position Number uniqueness rule reaches across Aggregate instances,
/// something no single loaded Aggregate can check about itself -- the identical
/// reasoning <c>IOrganizationRepository.ExistsWithCodeAsync</c> already states.
///
/// <see cref="WouldCreateCircularReportingAsync"/> exists because
/// <see cref="Position.ReportingPositionId"/> references another instance of this
/// SAME Aggregate type: detecting whether assigning a candidate reporting position
/// would make a Position its own transitive ancestor requires walking the candidate's
/// own reporting chain, a graph traversal only the Infrastructure layer can perform
/// (position-hierarchy.md: "Circular references are prohibited"). The Application
/// layer's command handler calls this before invoking
/// <see cref="Position.AssignReportingPosition"/>, per that method's own remarks.
/// </summary>
public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(PositionId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Position>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNumberAsync(Guid tenantId, string number, PositionId? excludeId, CancellationToken cancellationToken);

    Task<bool> WouldCreateCircularReportingAsync(
        Guid tenantId, Guid positionId, Guid candidateReportingPositionId, CancellationToken cancellationToken);

    Task AddAsync(Position position, CancellationToken cancellationToken);
}
