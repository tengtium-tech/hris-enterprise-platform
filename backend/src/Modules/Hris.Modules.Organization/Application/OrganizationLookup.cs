using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Application;

/// <summary>
/// The one place every command/query handler that loads an <see cref="Domain.Organization"/>,
/// <see cref="WorkLocation"/>, or <see cref="LegalEntity"/> by its own identifier
/// performs this module's own tenant-isolation check, per CTR-ISO-004. Returning a
/// not-found error for both a genuinely missing record and one that exists but
/// belongs to a different tenant is deliberate (CTR-ISO-002's own "not-found, not...
/// a permission error that confirms the record's existence" reasoning) -- the
/// identical shape <c>DocumentLookup</c>/<c>IntegrationLookup</c> already establish.
/// </summary>
internal static class OrganizationLookup
{
    public static async Task<Result<Domain.Organization>> LoadOrganizationForTenantAsync(
        IOrganizationRepository repository, Guid organizationId, Guid tenantId, CancellationToken cancellationToken)
    {
        var organization = await repository.GetByIdAsync(new OrganizationId(organizationId), cancellationToken)
            .ConfigureAwait(false);

        return organization is null || organization.TenantId != tenantId
            ? Result.Failure<Domain.Organization>(OrganizationErrors.OrganizationNotFound)
            : Result.Success(organization);
    }

    public static async Task<Result<WorkLocation>> LoadWorkLocationForTenantAsync(
        IWorkLocationRepository repository, Guid workLocationId, Guid tenantId, CancellationToken cancellationToken)
    {
        var workLocation = await repository.GetByIdAsync(new WorkLocationId(workLocationId), cancellationToken)
            .ConfigureAwait(false);

        return workLocation is null || workLocation.TenantId != tenantId
            ? Result.Failure<WorkLocation>(OrganizationErrors.WorkLocationNotFound)
            : Result.Success(workLocation);
    }

    public static async Task<Result<LegalEntity>> LoadLegalEntityForTenantAsync(
        ILegalEntityRepository repository, Guid legalEntityId, Guid tenantId, CancellationToken cancellationToken)
    {
        var legalEntity = await repository.GetByIdAsync(new LegalEntityId(legalEntityId), cancellationToken)
            .ConfigureAwait(false);

        return legalEntity is null || legalEntity.TenantId != tenantId
            ? Result.Failure<LegalEntity>(OrganizationErrors.LegalEntityNotFound)
            : Result.Success(legalEntity);
    }
}
