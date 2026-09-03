namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Repository contract for the <see cref="IssuedNumber"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
public interface IIssuedNumberRepository
{
    Task<IssuedNumber?> GetByIdAsync(IssuedNumberId id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<IssuedNumber>> GetBySeriesIdAsync(NumberSeriesId seriesId, CancellationToken cancellationToken);

    Task AddAsync(IssuedNumber issuedNumber, CancellationToken cancellationToken);
}
