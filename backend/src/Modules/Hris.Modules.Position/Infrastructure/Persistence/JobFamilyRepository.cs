using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Position.Infrastructure.Persistence;

internal sealed class JobFamilyRepository : IJobFamilyRepository
{
    private readonly HrisDbContext _dbContext;

    public JobFamilyRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<JobFamily?> GetByIdAsync(JobFamilyId id, CancellationToken cancellationToken) =>
        _dbContext.Set<JobFamily>().FirstOrDefaultAsync(jobFamily => jobFamily.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobFamily>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<JobFamily>()
            .Where(jobFamily => jobFamily.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithCodeAsync(
        Guid tenantId, string code, JobFamilyId? excludeId, CancellationToken cancellationToken)
    {
        var codeResult = JobFamilyCode.Create(code);
        if (codeResult.IsFailure)
        {
            return false;
        }

        var normalizedCode = codeResult.Value;
        return await _dbContext.Set<JobFamily>()
            .AnyAsync(
                jobFamily => jobFamily.TenantId == tenantId && jobFamily.Code == normalizedCode
                    && (excludeId == null || jobFamily.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNameAsync(
        Guid tenantId, string name, JobFamilyId? excludeId, CancellationToken cancellationToken)
    {
        var nameResult = JobFamilyName.Create(name);
        if (nameResult.IsFailure)
        {
            return false;
        }

        var normalizedName = nameResult.Value;
        return await _dbContext.Set<JobFamily>()
            .AnyAsync(
                jobFamily => jobFamily.TenantId == tenantId && jobFamily.Name == normalizedName
                    && (excludeId == null || jobFamily.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(JobFamily jobFamily, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(jobFamily, nameof(jobFamily));
        await _dbContext.Set<JobFamily>().AddAsync(jobFamily, cancellationToken).ConfigureAwait(false);
    }
}
