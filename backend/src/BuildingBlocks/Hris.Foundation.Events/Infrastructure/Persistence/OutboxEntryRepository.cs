using Hris.Foundation.Events.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Events.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IOutboxEntryRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split -- the identical shape <c>ConfigurationSettingRepository</c>/
/// <c>UserAccountRepository</c> already establish.
/// </summary>
/// <remarks>
/// UNVERIFIED (backend/README.md, "What hasn't been verified yet"): the owned
/// <see cref="EventEnvelope.Category"/>/<see cref="EventEnvelope.TenantId"/> filters in
/// <see cref="GetDeadLetteredAsync"/> reach into an owned-type navigation from a LINQ
/// predicate -- EF Core's own documentation shows this pattern (filtering on an owned
/// type's property) as supported, but this sandbox has no .NET SDK to compile and run
/// an integration test confirming the translation against the real PostgreSQL
/// provider, the identical caveat <c>ConfigurationSettingRepository</c>'s own remarks
/// state for its own Value Object comparisons.
/// </remarks>
internal sealed class OutboxEntryRepository : IOutboxEntryRepository
{
    private readonly HrisDbContext _dbContext;

    public OutboxEntryRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<OutboxEntry?> GetByIdAsync(OutboxEntryId id, CancellationToken cancellationToken) =>
        _dbContext.Set<OutboxEntry>()
            .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    public Task<IReadOnlyList<OutboxEntry>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken) =>
        QueryPendingBatchAsync(batchSize, cancellationToken);

    private async Task<IReadOnlyList<OutboxEntry>> QueryPendingBatchAsync(int batchSize, CancellationToken cancellationToken) =>
        await _dbContext.Set<OutboxEntry>()
            .Where(entry => entry.Status == OutboxEntryStatus.Pending)
            .OrderBy(entry => entry.Envelope.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<OutboxEntry>> GetDeadLetteredAsync(
        Guid? tenantId, int maxResults, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<OutboxEntry>()
            .Where(entry => entry.Status == OutboxEntryStatus.DeadLettered);

        if (tenantId is not null)
        {
            query = query.Where(entry => entry.Envelope.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(entry => entry.LastAttemptAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(OutboxEntry entry, CancellationToken cancellationToken) =>
        await _dbContext.Set<OutboxEntry>()
            .AddAsync(entry, cancellationToken)
            .ConfigureAwait(false);
}
