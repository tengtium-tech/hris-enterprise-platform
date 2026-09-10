using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IShiftAssignmentRepository"/>.</summary>
internal sealed class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly HrisDbContext _dbContext;

    public ShiftAssignmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<ShiftAssignment?> GetByIdAsync(ShiftAssignmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<ShiftAssignment>().FirstOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ShiftAssignment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ShiftAssignment>> ListByTargetAsync(
        Guid tenantId, string targetId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.TenantId == tenantId)
            .Where(assignment => assignment.TargetId == targetId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ShiftAssignment>> ListIndividualByEmployeeAsync(
        Guid tenantId, string employeeId, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.TenantId == tenantId)
            .Where(assignment => assignment.TargetId == employeeId)
            .Where(assignment => assignment.TargetLevel == OrganizationalAssignmentLevel.IndividualEmployee)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ShiftAssignment>> ListExpirableAsync(DateOnly asOfDate, CancellationToken cancellationToken) =>
        await _dbContext.Set<ShiftAssignment>()
            .Where(assignment => assignment.EffectiveTo != null && assignment.EffectiveTo < asOfDate)
            .Where(assignment => assignment.Status == ShiftAssignmentStatus.Scheduled
                                 || assignment.Status == ShiftAssignmentStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(assignment, nameof(assignment));
        await _dbContext.Set<ShiftAssignment>().AddAsync(assignment, cancellationToken).ConfigureAwait(false);
    }
}
