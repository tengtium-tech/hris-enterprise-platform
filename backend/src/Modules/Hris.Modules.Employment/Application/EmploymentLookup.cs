using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Employment.Application;

/// <summary>
/// The one place every command/query handler that loads an
/// <see cref="Domain.Employment"/>, <see cref="EmploymentContract"/>, or
/// <see cref="EmploymentAssignment"/> by its own identifier performs this module's
/// own tenant-isolation check, per CTR-ISO-004. Returning a not-found error for both
/// a genuinely missing record and one that exists but belongs to a different tenant
/// is deliberate (CTR-ISO-002), the identical shape <c>PositionLookup</c> and
/// <c>OrganizationLookup</c> already establish.
/// </summary>
internal static class EmploymentLookup
{
    public static async Task<Result<Domain.Employment>> LoadEmploymentForTenantAsync(
        IEmploymentRepository repository, Guid employmentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var employment = await repository.GetByIdAsync(new EmploymentId(employmentId), cancellationToken)
            .ConfigureAwait(false);

        return employment is null || employment.TenantId != tenantId
            ? Result.Failure<Domain.Employment>(EmploymentErrors.EmploymentNotFound)
            : Result.Success(employment);
    }

    public static async Task<Result<EmploymentContract>> LoadContractForTenantAsync(
        IEmploymentContractRepository repository, Guid contractId, Guid tenantId, CancellationToken cancellationToken)
    {
        var contract = await repository.GetByIdAsync(new EmploymentContractId(contractId), cancellationToken)
            .ConfigureAwait(false);

        return contract is null || contract.TenantId != tenantId
            ? Result.Failure<EmploymentContract>(EmploymentErrors.EmploymentContractNotFound)
            : Result.Success(contract);
    }

    public static async Task<Result<EmploymentAssignment>> LoadAssignmentForTenantAsync(
        IEmploymentAssignmentRepository repository, Guid assignmentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetByIdAsync(new EmploymentAssignmentId(assignmentId), cancellationToken)
            .ConfigureAwait(false);

        return assignment is null || assignment.TenantId != tenantId
            ? Result.Failure<EmploymentAssignment>(EmploymentErrors.EmploymentAssignmentNotFound)
            : Result.Success(assignment);
    }
}
