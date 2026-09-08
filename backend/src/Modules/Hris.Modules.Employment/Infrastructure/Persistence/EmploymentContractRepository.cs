using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Employment.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IEmploymentContractRepository"/>.
/// <see cref="HasValidContractAsync"/> treats "valid" as "exists, and has not been
/// Cancelled" -- employment-lifecycle.md's own Draft "requires an associated
/// Employment Contract" does not further specify which Contract Lifecycle stages
/// count, so a Draft or Approved contract (not yet Effective) also satisfies
/// activation's own precondition, matching how pre-boarding routinely signs a
/// contract before its own start date (employment-contracts.md's own "Approved is
/// distinct from Effective... which is what makes pre-boarding work").
/// </summary>
internal sealed class EmploymentContractRepository : IEmploymentContractRepository
{
    private readonly HrisDbContext _dbContext;

    public EmploymentContractRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<EmploymentContract?> GetByIdAsync(EmploymentContractId id, CancellationToken cancellationToken) =>
        _dbContext.Set<EmploymentContract>().FirstOrDefaultAsync(contract => contract.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EmploymentContract>> ListByEmploymentIdAsync(
        Guid tenantId, Guid employmentId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<EmploymentContract>()
            .Where(contract => contract.TenantId == tenantId && contract.EmploymentId == employmentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> HasValidContractAsync(Guid tenantId, Guid employmentId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmploymentContract>()
            .AnyAsync(
                contract => contract.TenantId == tenantId && contract.EmploymentId == employmentId
                    && contract.LifecycleStage != ContractLifecycleStage.Cancelled,
                cancellationToken);
    }

    public async Task AddAsync(EmploymentContract contract, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(contract, nameof(contract));
        await _dbContext.Set<EmploymentContract>().AddAsync(contract, cancellationToken).ConfigureAwait(false);
    }
}
