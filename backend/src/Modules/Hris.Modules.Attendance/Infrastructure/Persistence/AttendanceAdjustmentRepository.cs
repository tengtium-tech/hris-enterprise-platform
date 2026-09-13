using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAttendanceAdjustmentRepository"/>.</summary>
internal sealed class AttendanceAdjustmentRepository : IAttendanceAdjustmentRepository
{
    private readonly HrisDbContext _dbContext;

    public AttendanceAdjustmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<AttendanceAdjustment?> GetByIdAsync(AttendanceAdjustmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<AttendanceAdjustment>().FirstOrDefaultAsync(adjustment => adjustment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AttendanceAdjustment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendanceAdjustment>()
            .Where(adjustment => adjustment.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AttendanceAdjustment>> ListByRecordAsync(
        AttendanceRecordId recordId, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendanceAdjustment>()
            .Where(adjustment => adjustment.AttendanceRecordId == recordId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<AttendanceAdjustment?> GetPendingForRecordAsync(
        AttendanceRecordId recordId, CancellationToken cancellationToken) =>
        _dbContext.Set<AttendanceAdjustment>()
            .FirstOrDefaultAsync(
                adjustment => adjustment.AttendanceRecordId == recordId
                    && (adjustment.Status == AdjustmentStatus.Draft
                        || adjustment.Status == AdjustmentStatus.Submitted
                        || adjustment.Status == AdjustmentStatus.UnderReview
                        || adjustment.Status == AdjustmentStatus.Approved),
                cancellationToken);

    public async Task AddAsync(AttendanceAdjustment adjustment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(adjustment, nameof(adjustment));
        await _dbContext.Set<AttendanceAdjustment>().AddAsync(adjustment, cancellationToken).ConfigureAwait(false);
    }
}
