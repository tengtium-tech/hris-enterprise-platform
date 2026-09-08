namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Repository contract for the <see cref="EmploymentContract"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split.
///
/// <see cref="HasValidContractAsync"/> exists so the Application-layer command
/// handler can compute <see cref="Employment.Activate"/>'s own
/// <c>hasValidContract</c> boolean (EMP-003) -- <see cref="EmploymentContract"/> is a
/// separate Aggregate Root that <see cref="Employment"/> never reaches into
/// directly.
/// </summary>
public interface IEmploymentContractRepository
{
    Task<EmploymentContract?> GetByIdAsync(EmploymentContractId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmploymentContract>> ListByEmploymentIdAsync(
        Guid tenantId, Guid employmentId, CancellationToken cancellationToken);

    Task<bool> HasValidContractAsync(Guid tenantId, Guid employmentId, CancellationToken cancellationToken);

    Task AddAsync(EmploymentContract contract, CancellationToken cancellationToken);
}
