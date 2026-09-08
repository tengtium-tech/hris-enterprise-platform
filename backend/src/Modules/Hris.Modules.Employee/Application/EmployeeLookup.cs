using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Application;

/// <summary>
/// The one place every command/query handler that loads an
/// <see cref="Domain.Employee"/> by its own identifier performs this module's own
/// tenant-isolation check, per CTR-ISO-004. Returning a not-found error for both a
/// genuinely missing record and one that exists but belongs to a different tenant
/// is deliberate (CTR-ISO-002), the identical shape <c>EmploymentLookup</c> and
/// <c>PositionLookup</c> already establish.
/// </summary>
internal static class EmployeeLookup
{
    public static async Task<Result<Domain.Employee>> LoadEmployeeForTenantAsync(
        IEmployeeRepository repository, Guid employeeId, Guid tenantId, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken).ConfigureAwait(false);

        return employee is null || employee.TenantId != tenantId
            ? Result.Failure<Domain.Employee>(EmployeeErrors.EmployeeNotFound)
            : Result.Success(employee);
    }
}
