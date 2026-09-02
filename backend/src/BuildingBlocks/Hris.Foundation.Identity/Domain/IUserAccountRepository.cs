namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="UserAccount"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No Infrastructure implementation exists yet
/// (backend/README.md).
///
/// <see cref="GetByUsernameAsync"/> is what an Application-layer login handler calls
/// before invoking any <see cref="UserAccount"/> method -- a <c>null</c> result and a
/// found-account-with-wrong-password both become the identical generic authentication
/// failure at that layer, per identity-framework.md's "Never confirm whether an
/// account exists in a failed authentication response." This interface cannot enforce
/// that by itself; it is recorded here so the Application layer that eventually calls
/// it is not the first place the rule is written down.
/// </summary>
public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(UserAccountId id, Guid tenantId, CancellationToken cancellationToken);

    Task<UserAccount?> GetByUsernameAsync(Username username, Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Username username, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Total account count across every tenant, with no per-tenant breakdown -- added
    /// for Tenant Framework's own <c>GetPlatformDashboardSummaryQuery</c>
    /// (tenant-framework.md's own Returns column: "total UserAccount count across
    /// every tenant"). Identity Framework is one of Tenant Framework's own five
    /// stated Upstream Dependencies; this is a strictly additive read, the same
    /// "reference an already-built sibling framework concretely" precedent
    /// Localization Framework's own <c>ResolveTranslationQuery</c> already
    /// established for Configuration Framework.
    /// </summary>
    Task<int> CountAllAsync(CancellationToken cancellationToken);

    Task AddAsync(UserAccount account, CancellationToken cancellationToken);
}
