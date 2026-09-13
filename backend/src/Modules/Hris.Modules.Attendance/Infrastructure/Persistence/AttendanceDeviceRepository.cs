using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAttendanceDeviceRepository"/>.</summary>
internal sealed class AttendanceDeviceRepository : IAttendanceDeviceRepository
{
    private readonly HrisDbContext _dbContext;

    public AttendanceDeviceRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<AttendanceDevice?> GetByIdAsync(AttendanceDeviceId id, CancellationToken cancellationToken) =>
        _dbContext.Set<AttendanceDevice>().FirstOrDefaultAsync(device => device.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AttendanceDevice>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendanceDevice>()
            .Where(device => device.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<AttendanceDevice?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken) =>
        _dbContext.Set<AttendanceDevice>()
            .FirstOrDefaultAsync(device => device.SerialNumber == serialNumber, cancellationToken);

    public async Task<IReadOnlyList<AttendanceDevice>> ListActiveAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<AttendanceDevice>()
            .Where(device => device.TenantId == tenantId && device.Status == DeviceStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(AttendanceDevice device, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(device, nameof(device));
        await _dbContext.Set<AttendanceDevice>().AddAsync(device, cancellationToken).ConfigureAwait(false);
    }
}
