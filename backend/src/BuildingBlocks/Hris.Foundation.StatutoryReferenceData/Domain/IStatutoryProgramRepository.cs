namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface IStatutoryProgramRepository
{
    Task<StatutoryProgram?> GetByIdAsync(StatutoryProgramId id, CancellationToken cancellationToken);

    Task<StatutoryProgram?> GetByCodeAndCountryAsync(
        StatutoryProgramCode code, StatutoryCountryCode country, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAndCountryAsync(
        StatutoryProgramCode code, StatutoryCountryCode country, CancellationToken cancellationToken);

    Task<IReadOnlyList<StatutoryProgram>> ListByCountryAsync(
        StatutoryCountryCode country, CancellationToken cancellationToken);

    Task AddAsync(StatutoryProgram program, CancellationToken cancellationToken);
}
