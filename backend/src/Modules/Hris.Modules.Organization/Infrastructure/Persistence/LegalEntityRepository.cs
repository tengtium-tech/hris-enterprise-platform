using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ILegalEntityRepository"/>. See
/// <c>OrganizationRepository</c>'s own remarks for the shared "no explicit
/// <c>UpdateAsync</c>" and "compare against the constructed Value Object" reasoning.
/// LEG-001/LEG-002's own uniqueness has no <c>tenantId</c> parameter here, matching
/// <c>LegalEntityConfiguration</c>'s own unfiltered unique indexes.
/// </summary>
internal sealed class LegalEntityRepository : ILegalEntityRepository
{
    private readonly HrisDbContext _dbContext;

    public LegalEntityRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<LegalEntity?> GetByIdAsync(LegalEntityId id, CancellationToken cancellationToken) =>
        _dbContext.Set<LegalEntity>().FirstOrDefaultAsync(legalEntity => legalEntity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LegalEntity>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<LegalEntity>()
            .Where(legalEntity => legalEntity.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithBusinessRegistrationNumberAsync(
        string businessRegistrationNumber, LegalEntityId? excludeId, CancellationToken cancellationToken)
    {
        var numberResult = BusinessRegistrationNumber.Create(businessRegistrationNumber);
        if (numberResult.IsFailure)
        {
            return false;
        }

        var normalizedNumber = numberResult.Value;
        return await _dbContext.Set<LegalEntity>()
            .AnyAsync(
                legalEntity => legalEntity.BusinessRegistrationNumber == normalizedNumber
                    && (excludeId == null || legalEntity.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithTaxIdentificationNumberAsync(
        string taxIdentificationNumber, LegalEntityId? excludeId, CancellationToken cancellationToken)
    {
        var numberResult = TaxIdentificationNumber.Create(taxIdentificationNumber);
        if (numberResult.IsFailure)
        {
            return false;
        }

        var normalizedNumber = numberResult.Value;
        return await _dbContext.Set<LegalEntity>()
            .AnyAsync(
                legalEntity => legalEntity.TaxIdentificationNumber == normalizedNumber
                    && (excludeId == null || legalEntity.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(LegalEntity legalEntity, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(legalEntity, nameof(legalEntity));
        await _dbContext.Set<LegalEntity>().AddAsync(legalEntity, cancellationToken).ConfigureAwait(false);
    }
}
