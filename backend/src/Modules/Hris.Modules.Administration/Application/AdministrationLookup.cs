using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Administration.Application;

/// <summary>
/// The one place every command/query handler that loads a
/// <see cref="UserAccount"/>, <see cref="TenantRole"/>, or
/// <see cref="AdministrativeDelegation"/> by its own identifier performs this
/// module's own tenant-isolation check, per CTR-ISO-004. Returning a not-found
/// error for both a genuinely missing record and one that exists but belongs to a
/// different tenant is deliberate (CTR-ISO-002), the identical shape every prior
/// module's own lookup helper already establishes.
/// </summary>
internal static class AdministrationLookup
{
    public static async Task<Result<UserAccount>> LoadUserAccountForTenantAsync(
        IUserAccountRepository repository, Guid userAccountId, Guid tenantId, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(new UserAccountId(userAccountId), cancellationToken).ConfigureAwait(false);

        return account is null || account.TenantId != tenantId
            ? Result.Failure<UserAccount>(AdministrationErrors.UserAccountNotFound)
            : Result.Success(account);
    }

    public static async Task<Result<TenantRole>> LoadTenantRoleForTenantAsync(
        ITenantRoleRepository repository, Guid tenantRoleId, Guid tenantId, CancellationToken cancellationToken)
    {
        var role = await repository.GetByIdAsync(new TenantRoleId(tenantRoleId), cancellationToken).ConfigureAwait(false);

        return role is null || role.TenantId != tenantId
            ? Result.Failure<TenantRole>(AdministrationErrors.TenantRoleNotFound)
            : Result.Success(role);
    }

    public static async Task<Result<AdministrativeDelegation>> LoadDelegationForTenantAsync(
        IAdministrativeDelegationRepository repository, Guid delegationId, Guid tenantId, CancellationToken cancellationToken)
    {
        var delegation = await repository.GetByIdAsync(new DelegationId(delegationId), cancellationToken).ConfigureAwait(false);

        return delegation is null || delegation.TenantId != tenantId
            ? Result.Failure<AdministrativeDelegation>(AdministrationErrors.DelegationNotFound)
            : Result.Success(delegation);
    }
}
