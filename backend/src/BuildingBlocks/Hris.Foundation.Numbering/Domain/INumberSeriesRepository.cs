namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Repository contract for the <see cref="NumberSeries"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface INumberSeriesRepository
{
    Task<NumberSeries?> GetByIdAsync(NumberSeriesId id, CancellationToken cancellationToken);

    Task<NumberSeries?> GetByKeyAsync(SeriesKey key, CancellationToken cancellationToken);

    Task<bool> ExistsByKeyAsync(SeriesKey key, CancellationToken cancellationToken);

    Task AddAsync(NumberSeries numberSeries, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically advances <paramref name="seriesId"/>'s own running sequence by
    /// exactly one and returns the new value -- a single SQL statement
    /// (<c>UPDATE ... SET current_sequence_value = current_sequence_value + 1 ...
    /// RETURNING current_sequence_value</c>), never a load-mutate-save round trip
    /// through EF Core's own change tracker. <see cref="NumberSeries"/>'s own remarks
    /// explain why: two concurrent callers loading the same row and each computing
    /// "current value + 1" from their own stale read would both compute, and could both
    /// save, the identical next value -- exactly the collision the AI Implementation
    /// Guidance's "two simultaneous requests must never receive the same number"
    /// (CTR-DAT-001) prohibits. An atomic, single-statement database-side increment has
    /// no such window: PostgreSQL serializes concurrent writers to the same row, so the
    /// two concurrent callers each get a distinct, correctly-ordered result with no
    /// retry loop needed on either side. Verified against a real, disposable PostgreSQL
    /// instance under genuine concurrent load, not assumed correct from the SQL shape
    /// alone -- see <c>Hris.Infrastructure.IntegrationTests</c>'s own
    /// <c>NumberSeriesConcurrencyTests</c>.
    /// </summary>
    Task<long> IncrementAndGetNextSequenceValueAsync(NumberSeriesId seriesId, CancellationToken cancellationToken);
}
