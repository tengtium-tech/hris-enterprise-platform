using Hris.Foundation.Numbering.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Numbering.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IIssuedNumberRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class IssuedNumberRepository : IIssuedNumberRepository
{
    private readonly HrisDbContext _dbContext;

    public IssuedNumberRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<IssuedNumber?> GetByIdAsync(IssuedNumberId id, CancellationToken cancellationToken) =>
        _dbContext.Set<IssuedNumber>().FirstOrDefaultAsync(number => number.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<IssuedNumber>> GetBySeriesIdAsync(NumberSeriesId seriesId, CancellationToken cancellationToken) =>
        await _dbContext.Set<IssuedNumber>()
            .Where(number => number.NumberSeriesId == seriesId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(IssuedNumber issuedNumber, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(issuedNumber, nameof(issuedNumber));
        await _dbContext.Set<IssuedNumber>().AddAsync(issuedNumber, cancellationToken).ConfigureAwait(false);
    }
}
