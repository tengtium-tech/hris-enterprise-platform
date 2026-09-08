using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Employment.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IEmploymentAssignmentRepository"/>.
/// </summary>
internal sealed class EmploymentAssignmentRepository : IEmploymentAssignmentRepository
{
    private readonly HrisDbContext _dbContext;

    public EmploymentAssignmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<EmploymentAssignment?> GetByIdAsync(EmploymentAssignmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<EmploymentAssignment>().FirstOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public Task<EmploymentAssignment?> GetCurrentByEmploymentIdAsync(
        Guid tenantId, Guid employmentId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmploymentAssignment>()
            .FirstOrDefaultAsync(
                assignment => assignment.TenantId == tenantId && assignment.EmploymentId == employmentId
                    && !assignment.IsEnded,
                cancellationToken);
    }

    public Task<bool> HasValidAssignmentAsync(Guid tenantId, Guid employmentId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmploymentAssignment>()
            .AnyAsync(
                assignment => assignment.TenantId == tenantId && assignment.EmploymentId == employmentId
                    && !assignment.IsEnded,
                cancellationToken);
    }

    /// <summary>
    /// Walks <paramref name="candidateReportingManagerEmploymentId"/>'s own current
    /// reporting chain, looking for <paramref name="employmentId"/>. Finding it means
    /// assigning the candidate as <paramref name="employmentId"/>'s own reporting
    /// manager would make <paramref name="employmentId"/> its own transitive ancestor
    /// -- a cycle (ASG-006). The visited-set guard defensively stops the walk if it
    /// ever encounters a cycle elsewhere in already-persisted data, the identical
    /// pattern <c>PositionRepository.WouldCreateCircularReportingAsync</c> already
    /// establishes.
    /// </summary>
    public async Task<bool> WouldCreateCircularReportingAsync(
        Guid tenantId, Guid employmentId, Guid candidateReportingManagerEmploymentId, CancellationToken cancellationToken)
    {
        var currentId = candidateReportingManagerEmploymentId;
        var visited = new HashSet<Guid>();

        while (true)
        {
            if (currentId == employmentId)
            {
                return true;
            }

            if (!visited.Add(currentId))
            {
                return false;
            }

            var managerId = await _dbContext.Set<EmploymentAssignment>()
                .Where(assignment => assignment.TenantId == tenantId && assignment.EmploymentId == currentId
                    && !assignment.IsEnded)
                .Select(assignment => assignment.ReportingManagerEmploymentId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (managerId is null)
            {
                return false;
            }

            currentId = managerId.Value;
        }
    }

    public async Task AddAsync(EmploymentAssignment assignment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(assignment, nameof(assignment));
        await _dbContext.Set<EmploymentAssignment>().AddAsync(assignment, cancellationToken).ConfigureAwait(false);
    }
}
