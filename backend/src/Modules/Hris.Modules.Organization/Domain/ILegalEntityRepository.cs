namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Repository contract for the <see cref="LegalEntity"/> Aggregate Root. LEG-001 and
/// LEG-002's own uniqueness rules (business registration number and tax
/// identification number, both platform-wide per that document's own wording) are
/// checked via <see cref="ExistsWithBusinessRegistrationNumberAsync"/> and
/// <see cref="ExistsWithTaxIdentificationNumberAsync"/> by the Application layer
/// before <see cref="LegalEntity.Create"/> is called.
/// </summary>
public interface ILegalEntityRepository
{
    Task<LegalEntity?> GetByIdAsync(LegalEntityId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LegalEntity>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithBusinessRegistrationNumberAsync(
        string businessRegistrationNumber, LegalEntityId? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsWithTaxIdentificationNumberAsync(
        string taxIdentificationNumber, LegalEntityId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(LegalEntity legalEntity, CancellationToken cancellationToken);
}
