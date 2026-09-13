using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAttendanceRecordRepository"/>.</summary>
internal sealed class AttendanceRecordRepository : IAttendanceRecordRepository
{
    private readonly HrisDbContext _dbContext;

    public AttendanceRecordRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<AttendanceRecord?> GetByIdAsync(AttendanceRecordId id, CancellationToken cancellationToken) =>
        _dbContext.Set<AttendanceRecord>().FirstOrDefaultAsync(record => record.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AttendanceRecord>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendanceRecord>()
            .Where(record => record.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<AttendanceRecord?> GetByEmployeeAndWorkDateAsync(
        Guid tenantId, Guid employeeId, DateOnly workDate, CancellationToken cancellationToken) =>
        _dbContext.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(
                record => record.TenantId == tenantId && record.EmployeeId == employeeId && record.WorkDate == workDate,
                cancellationToken);

    public async Task<IReadOnlyList<AttendanceRecord>> GetFinalizedForPayrollPeriodAsync(
        Guid tenantId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendanceRecord>()
            .Where(record => record.TenantId == tenantId
                && record.Status == AttendanceStatus.Finalized
                && record.WorkDate >= periodStart
                && record.WorkDate <= periodEnd)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(record, nameof(record));
        await _dbContext.Set<AttendanceRecord>().AddAsync(record, cancellationToken).ConfigureAwait(false);
    }
}
