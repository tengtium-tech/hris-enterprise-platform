using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Authorization.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRoleAssignmentRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split -- the identical shape every other Sprint 3 framework's own
/// repository already establishes.
/// </summary>
internal sealed class RoleAssignmentRepository : IRoleAssignmentRepository
{
    private readonly HrisDbContext _dbContext;

    public RoleAssignmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<RoleAssignment?> GetByIdAsync(RoleAssignmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<RoleAssignment>()
            .FirstOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RoleAssignment>> GetByPrincipalAsync(UserAccountId principalId, CancellationToken cancellationToken) =>
        await _dbContext.Set<RoleAssignment>()
            .Where(assignment => assignment.PrincipalId == principalId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(RoleAssignment assignment, CancellationToken cancellationToken) =>
        await _dbContext.Set<RoleAssignment>()
            .AddAsync(assignment, cancellationToken)
            .ConfigureAwait(false);
}
