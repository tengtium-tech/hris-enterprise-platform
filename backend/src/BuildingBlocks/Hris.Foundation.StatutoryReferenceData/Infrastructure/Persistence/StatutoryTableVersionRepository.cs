using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.StatutoryReferenceData.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IStatutoryTableVersionRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class StatutoryTableVersionRepository : IStatutoryTableVersionRepository
{
    private readonly HrisDbContext _dbContext;

    public StatutoryTableVersionRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<StatutoryTableVersion?> GetByIdAsync(StatutoryTableVersionId id, CancellationToken cancellationToken) =>
        _dbContext.Set<StatutoryTableVersion>().FirstOrDefaultAsync(version => version.Id == id, cancellationToken);

    public Task<bool> ExistsByProgramAndVersionLabelAsync(
        StatutoryProgramId statutoryProgramId, StatutoryTableVersionLabel versionLabel, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(versionLabel, nameof(versionLabel));

        return _dbContext.Set<StatutoryTableVersion>().AnyAsync(
            version => version.StatutoryProgramId == statutoryProgramId && version.VersionLabel == versionLabel,
            cancellationToken);
    }

    public Task<StatutoryTableVersion?> GetLatestEffectiveAsOfAsync(
        StatutoryProgramId statutoryProgramId, DateTimeOffset periodUtc, CancellationToken cancellationToken) =>
        _dbContext.Set<StatutoryTableVersion>()
            .Where(version => version.StatutoryProgramId == statutoryProgramId && version.EffectiveFromUtc <= periodUtc)
            .OrderByDescending(version => version.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<StatutoryTableVersion>> ListByProgramAsync(
        StatutoryProgramId statutoryProgramId, CancellationToken cancellationToken) =>
        await _dbContext.Set<StatutoryTableVersion>()
            .Where(version => version.StatutoryProgramId == statutoryProgramId)
            .OrderByDescending(version => version.EffectiveFromUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(StatutoryTableVersion version, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(version, nameof(version));
        await _dbContext.Set<StatutoryTableVersion>().AddAsync(version, cancellationToken).ConfigureAwait(false);
    }
}
