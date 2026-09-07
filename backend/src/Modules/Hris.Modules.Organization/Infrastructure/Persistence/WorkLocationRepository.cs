using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IWorkLocationRepository"/>. See
/// <c>OrganizationRepository</c>'s own remarks for the shared "no explicit
/// <c>UpdateAsync</c>" and "compare against the constructed Value Object" reasoning.
/// </summary>
internal sealed class WorkLocationRepository : IWorkLocationRepository
{
    private readonly HrisDbContext _dbContext;

    public WorkLocationRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<WorkLocation?> GetByIdAsync(WorkLocationId id, CancellationToken cancellationToken) =>
        _dbContext.Set<WorkLocation>().FirstOrDefaultAsync(workLocation => workLocation.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkLocation>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<WorkLocation>()
            .Where(workLocation => workLocation.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithCodeAsync(
        Guid tenantId, string code, WorkLocationId? excludeId, CancellationToken cancellationToken)
    {
        var codeResult = LocationCode.Create(code);
        if (codeResult.IsFailure)
        {
            return false;
        }

        var normalizedCode = codeResult.Value;
        return await _dbContext.Set<WorkLocation>()
            .AnyAsync(
                workLocation => workLocation.TenantId == tenantId && workLocation.Code == normalizedCode
                    && (excludeId == null || workLocation.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(WorkLocation workLocation, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(workLocation, nameof(workLocation));
        await _dbContext.Set<WorkLocation>().AddAsync(workLocation, cancellationToken).ConfigureAwait(false);
    }
}
