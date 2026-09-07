namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Repository contract for the <see cref="Organization"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split. <see cref="GetByIdAsync"/> deliberately does not take a
/// tenant identifier, the same established pattern <c>IConnectorRepository</c> already
/// follows: the calling Application-layer handler verifies the loaded aggregate's own
/// <see cref="Organization.TenantId"/>, per CTR-ISO-004.
///
/// The four <c>Exists*</c> methods exist because ORG-001/ORG-002 (Organization name
/// and code, tenant-wide) and DEPT-002/CC-001 (Department code, Cost Center code,
/// both tenant-wide) are uniqueness rules that reach *across* Organization Aggregate
/// instances -- something no single loaded Aggregate can check about itself.
/// Application-layer command handlers call these before invoking the corresponding
/// Aggregate factory or method, the same split <c>DocumentLookup</c> already
/// establishes for cross-cutting tenant checks.
/// </summary>
public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Organization>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsWithCodeAsync(Guid tenantId, string code, OrganizationId? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(Guid tenantId, string name, OrganizationId? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsDepartmentWithCodeAsync(
        Guid tenantId, string departmentCode, DepartmentId? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsCostCenterWithCodeAsync(
        Guid tenantId, string costCenterCode, CostCenterId? excludeId, CancellationToken cancellationToken);

    Task AddAsync(Organization organization, CancellationToken cancellationToken);
}
