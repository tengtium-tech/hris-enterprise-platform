using Hris.Modules.Position.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Position.Application;

/// <summary>
/// The one place every command/query handler that loads a
/// <see cref="Domain.Position"/>, <see cref="JobFamily"/>,
/// <see cref="JobClassification"/>, or <see cref="JobGrade"/> by its own identifier
/// performs this module's own tenant-isolation check, per CTR-ISO-004. Returning a
/// not-found error for both a genuinely missing record and one that exists but
/// belongs to a different tenant is deliberate (CTR-ISO-002), the identical shape
/// <c>OrganizationLookup</c> already establishes.
/// </summary>
internal static class PositionLookup
{
    public static async Task<Result<Domain.Position>> LoadPositionForTenantAsync(
        IPositionRepository repository, Guid positionId, Guid tenantId, CancellationToken cancellationToken)
    {
        var position = await repository.GetByIdAsync(new PositionId(positionId), cancellationToken).ConfigureAwait(false);

        return position is null || position.TenantId != tenantId
            ? Result.Failure<Domain.Position>(PositionErrors.PositionNotFound)
            : Result.Success(position);
    }

    public static async Task<Result<JobFamily>> LoadJobFamilyForTenantAsync(
        IJobFamilyRepository repository, Guid jobFamilyId, Guid tenantId, CancellationToken cancellationToken)
    {
        var jobFamily = await repository.GetByIdAsync(new JobFamilyId(jobFamilyId), cancellationToken).ConfigureAwait(false);

        return jobFamily is null || jobFamily.TenantId != tenantId
            ? Result.Failure<JobFamily>(PositionErrors.JobFamilyNotFound)
            : Result.Success(jobFamily);
    }

    public static async Task<Result<JobClassification>> LoadJobClassificationForTenantAsync(
        IJobClassificationRepository repository, Guid jobClassificationId, Guid tenantId, CancellationToken cancellationToken)
    {
        var jobClassification = await repository.GetByIdAsync(new JobClassificationId(jobClassificationId), cancellationToken)
            .ConfigureAwait(false);

        return jobClassification is null || jobClassification.TenantId != tenantId
            ? Result.Failure<JobClassification>(PositionErrors.JobClassificationNotFound)
            : Result.Success(jobClassification);
    }

    public static async Task<Result<JobGrade>> LoadJobGradeForTenantAsync(
        IJobGradeRepository repository, Guid jobGradeId, Guid tenantId, CancellationToken cancellationToken)
    {
        var jobGrade = await repository.GetByIdAsync(new JobGradeId(jobGradeId), cancellationToken).ConfigureAwait(false);

        return jobGrade is null || jobGrade.TenantId != tenantId
            ? Result.Failure<JobGrade>(PositionErrors.JobGradeNotFound)
            : Result.Success(jobGrade);
    }
}
