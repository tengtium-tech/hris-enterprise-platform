namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Repository contract for the <see cref="Document"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. <see cref="GetByIdAsync"/> deliberately does not take a
/// tenant identifier -- per this codebase's own established pattern (see, for example,
/// <c>IJobRepository.GetByIdAsync</c>), a repository loads by surrogate key alone, and
/// the calling Application-layer handler verifies the loaded aggregate's own
/// <see cref="Document.TenantId"/> before returning anything, per CTR-ISO-002.
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Document> Items, int TotalCount)> SearchAsync(
        Guid tenantId,
        string? category,
        DocumentClassification? classification,
        string? titleContains,
        IReadOnlyList<string>? tags,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task AddAsync(Document document, CancellationToken cancellationToken);
}
