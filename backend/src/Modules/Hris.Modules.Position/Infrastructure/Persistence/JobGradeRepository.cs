using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Position.Infrastructure.Persistence;

internal sealed class JobGradeRepository : IJobGradeRepository
{
    private readonly HrisDbContext _dbContext;

    public JobGradeRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<JobGrade?> GetByIdAsync(JobGradeId id, CancellationToken cancellationToken) =>
        _dbContext.Set<JobGrade>().FirstOrDefaultAsync(jobGrade => jobGrade.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobGrade>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<JobGrade>()
            .Where(jobGrade => jobGrade.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithCodeAsync(
        Guid tenantId, string code, JobGradeId? excludeId, CancellationToken cancellationToken)
    {
        var codeResult = JobGradeCode.Create(code);
        if (codeResult.IsFailure)
        {
            return false;
        }

        var normalizedCode = codeResult.Value;
        return await _dbContext.Set<JobGrade>()
            .AnyAsync(
                jobGrade => jobGrade.TenantId == tenantId && jobGrade.Code == normalizedCode
                    && (excludeId == null || jobGrade.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNameAsync(
        Guid tenantId, string name, JobGradeId? excludeId, CancellationToken cancellationToken)
    {
        var nameResult = JobGradeName.Create(name);
        if (nameResult.IsFailure)
        {
            return false;
        }

        var normalizedName = nameResult.Value;
        return await _dbContext.Set<JobGrade>()
            .AnyAsync(
                jobGrade => jobGrade.TenantId == tenantId && jobGrade.Name == normalizedName
                    && (excludeId == null || jobGrade.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(JobGrade jobGrade, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(jobGrade, nameof(jobGrade));
        await _dbContext.Set<JobGrade>().AddAsync(jobGrade, cancellationToken).ConfigureAwait(false);
    }
}
