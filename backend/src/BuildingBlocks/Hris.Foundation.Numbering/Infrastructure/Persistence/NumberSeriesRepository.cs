using Hris.Foundation.Numbering.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hris.Foundation.Numbering.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="INumberSeriesRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No <c>UpdateAsync</c>: an aggregate loaded through
/// <see cref="GetByIdAsync"/> is already tracked by this same <see cref="HrisDbContext"/>,
/// so the caller's own <c>TransactionBehavior</c> persists any mutation via change
/// tracking alone -- except <see cref="IncrementAndGetNextSequenceValueAsync"/>, which
/// deliberately bypasses that same change tracker; see its own remarks.
/// </summary>
internal sealed class NumberSeriesRepository : INumberSeriesRepository
{
    private readonly HrisDbContext _dbContext;

    public NumberSeriesRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<NumberSeries?> GetByIdAsync(NumberSeriesId id, CancellationToken cancellationToken) =>
        _dbContext.Set<NumberSeries>().FirstOrDefaultAsync(series => series.Id == id, cancellationToken);

    public Task<NumberSeries?> GetByKeyAsync(SeriesKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        return _dbContext.Set<NumberSeries>().FirstOrDefaultAsync(series => series.Key == key, cancellationToken);
    }

    public Task<bool> ExistsByKeyAsync(SeriesKey key, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(key, nameof(key));

        return _dbContext.Set<NumberSeries>().AnyAsync(series => series.Key == key, cancellationToken);
    }

    public async Task AddAsync(NumberSeries numberSeries, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(numberSeries, nameof(numberSeries));
        await _dbContext.Set<NumberSeries>().AddAsync(numberSeries, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// A single, atomic <c>UPDATE ... RETURNING</c> statement -- see
    /// <see cref="INumberSeriesRepository.IncrementAndGetNextSequenceValueAsync"/>'s own
    /// remarks for why this must never be a load-mutate-save round trip through EF
    /// Core's change tracker instead. Executed via <c>Database.SqlQueryRaw</c> against
    /// the same connection/transaction this <see cref="HrisDbContext"/> already
    /// participates in -- not a second, separate connection -- so it composes correctly
    /// with whatever transaction <c>TransactionBehavior</c> has already opened around
    /// this same unit of work, and is not left dangling if that transaction later rolls
    /// back.
    ///
    /// The caller is still responsible for calling
    /// <see cref="NumberSeries.ReconcileSequenceValueAfterAtomicIncrement"/> on any
    /// already-tracked instance of this same row within the same unit of work -- this
    /// method's own raw SQL bypasses the change tracker entirely, so a tracked
    /// <see cref="NumberSeries"/> would otherwise still show the pre-increment value in
    /// memory even though the database row has already moved on. A subsequent
    /// <c>SaveChangesAsync</c> then writes that same, now-reconciled value back through
    /// the ordinary EF Core path -- a harmless, deliberately-accepted redundant write of
    /// the identical value this method already committed, not a second increment.
    /// </summary>
    public async Task<long> IncrementAndGetNextSequenceValueAsync(NumberSeriesId seriesId, CancellationToken cancellationToken)
    {
        var results = await _dbContext.Database
            .SqlQueryRaw<long>(
                """
                UPDATE number_series
                SET current_sequence_value = current_sequence_value + 1
                WHERE id = @seriesId
                RETURNING current_sequence_value
                """,
                new NpgsqlParameter("seriesId", seriesId.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results.Count == 0
            ? throw new InvalidOperationException($"No number series exists for id '{seriesId.Value}' to increment.")
            : results[0];
    }
}
