using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Position.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IPositionRepository"/>. See
/// <c>OrganizationRepository</c>'s own remarks for the shared "no explicit
/// <c>UpdateAsync</c>" and "compare against the constructed Value Object" reasoning.
/// </summary>
internal sealed class PositionRepository : IPositionRepository
{
    private readonly HrisDbContext _dbContext;

    public PositionRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Domain.Position?> GetByIdAsync(PositionId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Domain.Position>().FirstOrDefaultAsync(position => position.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Position>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Domain.Position>()
            .Where(position => position.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNumberAsync(
        Guid tenantId, string number, PositionId? excludeId, CancellationToken cancellationToken)
    {
        var numberResult = PositionNumber.Create(number);
        if (numberResult.IsFailure)
        {
            return false;
        }

        var normalizedNumber = numberResult.Value;
        return await _dbContext.Set<Domain.Position>()
            .AnyAsync(
                position => position.TenantId == tenantId && position.Number == normalizedNumber
                    && (excludeId == null || position.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Walks <paramref name="candidateReportingPositionId"/>'s own reporting chain,
    /// looking for <paramref name="positionId"/>. Finding it means assigning the
    /// candidate as <paramref name="positionId"/>'s own reporting position would make
    /// <paramref name="positionId"/> its own transitive ancestor -- a cycle
    /// (position-hierarchy.md: "Circular references are prohibited"). The visited-set
    /// guard defensively stops the walk if it ever encounters a cycle elsewhere in
    /// already-persisted data, rather than looping forever.
    /// </summary>
    public async Task<bool> WouldCreateCircularReportingAsync(
        Guid tenantId, Guid positionId, Guid candidateReportingPositionId, CancellationToken cancellationToken)
    {
        var currentId = candidateReportingPositionId;
        var visited = new HashSet<Guid>();

        while (true)
        {
            if (currentId == positionId)
            {
                return true;
            }

            if (!visited.Add(currentId))
            {
                return false;
            }

            var nextId = new PositionId(currentId);
            var parentId = await _dbContext.Set<Domain.Position>()
                .Where(position => position.TenantId == tenantId && position.Id == nextId)
                .Select(position => position.ReportingPositionId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (parentId is null)
            {
                return false;
            }

            currentId = parentId.Value;
        }
    }

    public async Task AddAsync(Domain.Position position, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(position, nameof(position));
        await _dbContext.Set<Domain.Position>().AddAsync(position, cancellationToken).ConfigureAwait(false);
    }
}
