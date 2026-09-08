using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Position.Infrastructure.Persistence;

internal sealed class JobClassificationRepository : IJobClassificationRepository
{
    private readonly HrisDbContext _dbContext;

    public JobClassificationRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<JobClassification?> GetByIdAsync(JobClassificationId id, CancellationToken cancellationToken) =>
        _dbContext.Set<JobClassification>().FirstOrDefaultAsync(jobClassification => jobClassification.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobClassification>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<JobClassification>()
            .Where(jobClassification => jobClassification.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithCodeAsync(
        Guid tenantId, string code, JobClassificationId? excludeId, CancellationToken cancellationToken)
    {
        var codeResult = JobClassificationCode.Create(code);
        if (codeResult.IsFailure)
        {
            return false;
        }

        var normalizedCode = codeResult.Value;
        return await _dbContext.Set<JobClassification>()
            .AnyAsync(
                jobClassification => jobClassification.TenantId == tenantId && jobClassification.Code == normalizedCode
                    && (excludeId == null || jobClassification.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNameAsync(
        Guid tenantId, string name, JobClassificationId? excludeId, CancellationToken cancellationToken)
    {
        var nameResult = JobClassificationName.Create(name);
        if (nameResult.IsFailure)
        {
            return false;
        }

        var normalizedName = nameResult.Value;
        return await _dbContext.Set<JobClassification>()
            .AnyAsync(
                jobClassification => jobClassification.TenantId == tenantId && jobClassification.Name == normalizedName
                    && (excludeId == null || jobClassification.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(JobClassification jobClassification, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(jobClassification, nameof(jobClassification));
        await _dbContext.Set<JobClassification>().AddAsync(jobClassification, cancellationToken).ConfigureAwait(false);
    }
}
