using Hris.Infrastructure.Persistence;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Employee.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IEmployeeHistoryRepository"/>.
/// </summary>
internal sealed class EmployeeHistoryRepository : IEmployeeHistoryRepository
{
    private readonly HrisDbContext _dbContext;

    public EmployeeHistoryRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public async Task<IReadOnlyList<EmployeeHistory>> ListByEmployeeIdAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<EmployeeHistory>()
            .Where(history => history.TenantId == tenantId && history.EmployeeId == employeeId)
            .OrderBy(history => history.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(EmployeeHistory history, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(history, nameof(history));
        await _dbContext.Set<EmployeeHistory>().AddAsync(history, cancellationToken).ConfigureAwait(false);
    }
}
