using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IBiometricEnrollmentRepository"/>.</summary>
internal sealed class BiometricEnrollmentRepository : IBiometricEnrollmentRepository
{
    private readonly HrisDbContext _dbContext;

    public BiometricEnrollmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<BiometricEnrollment?> GetByIdAsync(BiometricEnrollmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<BiometricEnrollment>().FirstOrDefaultAsync(enrollment => enrollment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BiometricEnrollment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<BiometricEnrollment>()
            .Where(enrollment => enrollment.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<BiometricEnrollment?> GetByEmployeeAndMethodAsync(
        Guid tenantId, Guid employeeId, BiometricMethod method, CancellationToken cancellationToken) =>
        _dbContext.Set<BiometricEnrollment>()
            .FirstOrDefaultAsync(enrollment => enrollment.TenantId == tenantId
                && enrollment.EmployeeId == employeeId
                && enrollment.Method == method,
                cancellationToken);

    public async Task<IReadOnlyList<BiometricEnrollment>> ListActiveForEmployeeAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken) =>
        await _dbContext.Set<BiometricEnrollment>()
            .Where(enrollment => enrollment.TenantId == tenantId
                && enrollment.EmployeeId == employeeId
                && enrollment.Status == EnrollmentStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(BiometricEnrollment enrollment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(enrollment, nameof(enrollment));
        await _dbContext.Set<BiometricEnrollment>().AddAsync(enrollment, cancellationToken).ConfigureAwait(false);
    }
}
