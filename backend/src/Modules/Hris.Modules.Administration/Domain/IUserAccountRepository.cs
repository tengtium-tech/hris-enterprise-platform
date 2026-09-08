namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Repository contract for the <see cref="UserAccount"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split. <see cref="GetByIdAsync"/> deliberately does not take a
/// tenant identifier, the same established pattern every prior module's own
/// repository follows: the calling Application-layer handler verifies the loaded
/// aggregate's own <see cref="UserAccount.TenantId"/>.
///
/// <see cref="CountOtherActiveTenantAdministratorsAsync"/> exists because AR-014's
/// "last tenant administrator" guard must be checked by counting active accounts
/// with an effective assignment across the whole tenant, not by inspecting a
/// single loaded aggregate.
/// </summary>
public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(UserAccountId id, CancellationToken cancellationToken);

    Task<UserAccount?> GetByEmployeeIdAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserAccount>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<int> CountOtherActiveTenantAdministratorsAsync(Guid tenantId, Guid excludingAccountId, CancellationToken cancellationToken);

    Task<bool> HasActiveAssignmentForTenantRoleAsync(Guid tenantId, Guid tenantRoleId, CancellationToken cancellationToken);

    Task AddAsync(UserAccount account, CancellationToken cancellationToken);
}
