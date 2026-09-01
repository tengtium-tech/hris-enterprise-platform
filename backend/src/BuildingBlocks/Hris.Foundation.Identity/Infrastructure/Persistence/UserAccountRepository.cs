using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUserAccountRepository"/>, per
/// docs/02-architecture/04-domain-driven-design/repositories.md's "Repository
/// interfaces belong in the Domain layer... Implementation in Infrastructure."
///
/// Every read loads the full Aggregate (its owned <c>Sessions</c>/<c>MfaFactors</c>
/// collections included automatically by EF Core's own Owned Entity default -- no
/// explicit <c>.Include()</c> call, the same as <c>ConfigurationSettingRepository</c>'s
/// own <c>Versions</c> load) rather than projecting, per aggregate-persistence.md's
/// Loading Aggregates section: every caller of this repository calls a behavior method
/// on the loaded Aggregate, which is exactly the "business behavior required" case
/// that section reserves full-Aggregate loads for.
/// </summary>
/// <remarks>
/// VERIFIED: both <c>account.Username == username</c> and
/// <c>account.TenantId == tenantId</c> below compare a converted property to a
/// constant -- the identical shape <c>ConfigurationSettingRepository</c>'s own
/// remarks already confirmed (HEP-38). Confirmed here too, against a real,
/// disposable PostgreSQL 16 instance via Testcontainers -- see
/// <c>Hris.Infrastructure.IntegrationTests.RepositoryQueryTranslationTests.UserAccountRepository_GetByUsernameAsync_TranslatesUsernameAndTenantIdComparison</c>.
/// Passes: no fix needed.
/// </remarks>
internal sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly HrisDbContext _dbContext;

    public UserAccountRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<UserAccount?> GetByIdAsync(UserAccountId id, Guid tenantId, CancellationToken cancellationToken) =>
        _dbContext.Set<UserAccount>()
            .FirstOrDefaultAsync(account => account.Id == id && account.TenantId == tenantId, cancellationToken);

    public Task<UserAccount?> GetByUsernameAsync(Username username, Guid tenantId, CancellationToken cancellationToken) =>
        _dbContext.Set<UserAccount>()
            .FirstOrDefaultAsync(account => account.Username == username && account.TenantId == tenantId, cancellationToken);

    public Task<bool> ExistsAsync(Username username, Guid tenantId, CancellationToken cancellationToken) =>
        _dbContext.Set<UserAccount>()
            .AnyAsync(account => account.Username == username && account.TenantId == tenantId, cancellationToken);

    public async Task AddAsync(UserAccount account, CancellationToken cancellationToken) =>
        await _dbContext.Set<UserAccount>()
            .AddAsync(account, cancellationToken)
            .ConfigureAwait(false);
}
