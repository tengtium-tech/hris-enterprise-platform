namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// Persistence abstraction for the <see cref="RuleDefinition"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. No Infrastructure implementation exists yet
/// (backend/README.md).
/// </summary>
public interface IRuleDefinitionRepository
{
    Task<RuleDefinition?> GetByIdAsync(RuleDefinitionId id, CancellationToken cancellationToken);

    Task<RuleDefinition?> GetByKeyAsync(RuleKey key, CancellationToken cancellationToken);

    Task AddAsync(RuleDefinition definition, CancellationToken cancellationToken);
}
