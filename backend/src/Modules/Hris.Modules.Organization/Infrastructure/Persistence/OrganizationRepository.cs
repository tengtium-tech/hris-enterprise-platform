using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IOrganizationRepository"/>, per
/// repositories.md's "interface in the Domain layer, implementation in
/// Infrastructure" split. No <c>UpdateAsync</c>: an aggregate loaded through
/// <see cref="GetByIdAsync"/> is already tracked by this same
/// <see cref="HrisDbContext"/>, so the caller's own <c>TransactionBehavior</c>
/// persists any mutation via change tracking alone -- the identical shape
/// <c>ConnectorRepository</c> already establishes. <see cref="GetByIdAsync"/> needs
/// no explicit <c>Include</c>: EF Core's own owned-type model
/// (<c>OrganizationConfiguration</c>'s <c>OwnsMany</c> chain) always loads every
/// nested BusinessUnit/Division/Department/Section/Team/CostCenter together with
/// their owner, by design.
///
/// The <c>Exists*</c> queries compare against the already-constructed Value Object
/// (<c>o.Code == normalizedCode</c>), not a raw string dereferenced through
/// <c>.Value</c> inside the LINQ expression, mirroring
/// <c>UserAccountRepository.ExistsByUsernameAsync</c>'s own verified translation
/// pattern for a single-column <c>HasConversion</c>-mapped Value Object.
/// <see cref="ExistsWithNameAsync"/> compares case-sensitively: a cross-provider
/// case-insensitive comparison against a converted Value Object's own nested
/// property is a real, separate translation question this Sprint does not need to
/// answer, since ORG-001 itself does not state its own uniqueness must be
/// case-insensitive.
/// </summary>
internal sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly HrisDbContext _dbContext;

    public OrganizationRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Domain.Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Domain.Organization>().FirstOrDefaultAsync(organization => organization.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Organization>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Domain.Organization>()
            .Where(organization => organization.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithCodeAsync(
        Guid tenantId, string code, OrganizationId? excludeId, CancellationToken cancellationToken)
    {
        var codeResult = OrganizationCode.Create(code);
        if (codeResult.IsFailure)
        {
            return false;
        }

        var normalizedCode = codeResult.Value;
        return await _dbContext.Set<Domain.Organization>()
            .AnyAsync(
                organization => organization.TenantId == tenantId && organization.Code == normalizedCode
                    && (excludeId == null || organization.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNameAsync(
        Guid tenantId, string name, OrganizationId? excludeId, CancellationToken cancellationToken)
    {
        var nameResult = OrganizationName.Create(name);
        if (nameResult.IsFailure)
        {
            return false;
        }

        var normalizedName = nameResult.Value;
        return await _dbContext.Set<Domain.Organization>()
            .AnyAsync(
                organization => organization.TenantId == tenantId && organization.Name == normalizedName
                    && (excludeId == null || organization.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsDepartmentWithCodeAsync(
        Guid tenantId, string departmentCode, DepartmentId? excludeId, CancellationToken cancellationToken)
    {
        var codeResult = DepartmentCode.Create(departmentCode);
        if (codeResult.IsFailure)
        {
            return false;
        }

        var normalizedCode = codeResult.Value;
        var organizations = await _dbContext.Set<Domain.Organization>()
            .Where(organization => organization.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return organizations
            .SelectMany(o => o.BusinessUnits)
            .SelectMany(bu => bu.Divisions)
            .SelectMany(d => d.Departments)
            .Any(dep => dep.Code == normalizedCode && (excludeId is null || dep.Id != excludeId));
    }

    public async Task<bool> ExistsCostCenterWithCodeAsync(
        Guid tenantId, string costCenterCode, CostCenterId? excludeId, CancellationToken cancellationToken)
    {
        var codeResult = CostCenterCode.Create(costCenterCode);
        if (codeResult.IsFailure)
        {
            return false;
        }

        var normalizedCode = codeResult.Value;
        var organizations = await _dbContext.Set<Domain.Organization>()
            .Where(organization => organization.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return organizations
            .SelectMany(o => o.CostCenters)
            .Any(cc => cc.Code == normalizedCode && (excludeId is null || cc.Id != excludeId));
    }

    public async Task AddAsync(Domain.Organization organization, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(organization, nameof(organization));
        await _dbContext.Set<Domain.Organization>().AddAsync(organization, cancellationToken).ConfigureAwait(false);
    }
}
