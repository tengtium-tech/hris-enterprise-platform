using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.StatutoryReferenceData.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IStatutoryProgramRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class StatutoryProgramRepository : IStatutoryProgramRepository
{
    private readonly HrisDbContext _dbContext;

    public StatutoryProgramRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<StatutoryProgram?> GetByIdAsync(StatutoryProgramId id, CancellationToken cancellationToken) =>
        _dbContext.Set<StatutoryProgram>().FirstOrDefaultAsync(program => program.Id == id, cancellationToken);

    public Task<StatutoryProgram?> GetByCodeAndCountryAsync(
        StatutoryProgramCode code, StatutoryCountryCode country, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(code, nameof(code));
        Guard.AgainstNull(country, nameof(country));

        return _dbContext.Set<StatutoryProgram>()
            .FirstOrDefaultAsync(program => program.Code == code && program.Country == country, cancellationToken);
    }

    public Task<bool> ExistsByCodeAndCountryAsync(
        StatutoryProgramCode code, StatutoryCountryCode country, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(code, nameof(code));
        Guard.AgainstNull(country, nameof(country));

        return _dbContext.Set<StatutoryProgram>()
            .AnyAsync(program => program.Code == code && program.Country == country, cancellationToken);
    }

    public async Task<IReadOnlyList<StatutoryProgram>> ListByCountryAsync(
        StatutoryCountryCode country, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(country, nameof(country));

        return await _dbContext.Set<StatutoryProgram>()
            .Where(program => program.Country == country)
            .OrderBy(program => program.Code.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(StatutoryProgram program, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(program, nameof(program));
        await _dbContext.Set<StatutoryProgram>().AddAsync(program, cancellationToken).ConfigureAwait(false);
    }
}
