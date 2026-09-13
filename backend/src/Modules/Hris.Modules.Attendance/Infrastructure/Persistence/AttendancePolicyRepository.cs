using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAttendancePolicyRepository"/>.</summary>
internal sealed class AttendancePolicyRepository : IAttendancePolicyRepository
{
    private readonly HrisDbContext _dbContext;

    public AttendancePolicyRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<AttendancePolicy?> GetByIdAsync(AttendancePolicyId id, CancellationToken cancellationToken) =>
        _dbContext.Set<AttendancePolicy>().FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AttendancePolicy>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendancePolicy>()
            .Where(policy => policy.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AttendancePolicy>> ListByLineageAsync(Guid lineageId, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendancePolicy>()
            .Where(policy => policy.LineageId == lineageId)
            .OrderBy(policy => policy.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AttendancePolicy>> ListEffectiveForWorkDateAsync(
        Guid tenantId, DateOnly workDate, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendancePolicy>()
            .Where(policy => policy.TenantId == tenantId
                && policy.Status == AttendancePolicyStatus.Active
                && policy.EffectiveFrom <= workDate
                && (policy.EffectiveTo == null || workDate <= policy.EffectiveTo.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(AttendancePolicy policy, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(policy, nameof(policy));
        await _dbContext.Set<AttendancePolicy>().AddAsync(policy, cancellationToken).ConfigureAwait(false);
    }
}
