using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUserAccountRepository"/>.
/// </summary>
internal sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly HrisDbContext _dbContext;

    public UserAccountRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<UserAccount?> GetByIdAsync(UserAccountId id, CancellationToken cancellationToken) =>
        _dbContext.Set<UserAccount>().FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public Task<UserAccount?> GetByEmployeeIdAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<UserAccount>()
            .FirstOrDefaultAsync(account => account.TenantId == tenantId && account.EmployeeId == employeeId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserAccount>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<UserAccount>()
            .Where(account => account.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> CountOtherActiveTenantAdministratorsAsync(
        Guid tenantId, Guid excludingAccountId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var accounts = await _dbContext.Set<UserAccount>()
            .Where(account => account.TenantId == tenantId && account.Id != new UserAccountId(excludingAccountId)
                && account.Status == UserAccountStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return accounts.Count(account => account.HoldsEffectiveSystemAdministratorAtTenantScope(today));
    }

    public async Task<bool> HasActiveAssignmentForTenantRoleAsync(Guid tenantId, Guid tenantRoleId, CancellationToken cancellationToken)
    {
        var accounts = await _dbContext.Set<UserAccount>()
            .Where(account => account.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return accounts.Any(account => account.RoleAssignments.Any(
            assignment => assignment.RevokedOn is null && assignment.Role.Kind == RoleKind.Tenant
                && assignment.Role.TenantRoleId == tenantRoleId));
    }

    public async Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(account, nameof(account));
        await _dbContext.Set<UserAccount>().AddAsync(account, cancellationToken).ConfigureAwait(false);
    }
}
