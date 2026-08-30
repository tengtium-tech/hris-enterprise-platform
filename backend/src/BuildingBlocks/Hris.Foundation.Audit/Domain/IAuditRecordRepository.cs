namespace Hris.Foundation.Audit.Domain;

/// <summary>
/// Persistence abstraction for <see cref="AuditRecord"/>, per repositories.md's
/// "interface in the Domain layer... implementation in Infrastructure" split. No
/// Infrastructure implementation exists yet (backend/README.md).
///
/// Deliberately has no <c>Update</c>/<c>Remove</c> method -- the second, structural
/// half of `CTR-AUD-001` alongside <see cref="AuditRecord"/>'s own immutability: even
/// an Infrastructure implementation with direct database access has no interface
/// method here that would let it modify or delete a persisted record. Search and
/// reporting are intentionally not here either, per repositories.md's own "Query
/// Separation" ("Repositories should return Aggregates... Read models... should use
/// dedicated Query Services") -- see <see cref="IAuditSearchService"/>.
/// </summary>
public interface IAuditRecordRepository
{
    Task<AuditRecord?> GetByIdAsync(AuditRecordId id, CancellationToken cancellationToken);

    Task AddAsync(AuditRecord record, CancellationToken cancellationToken);
}
