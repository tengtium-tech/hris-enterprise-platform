namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Repository contract for the <see cref="DocumentAttachment"/> Aggregate Root, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split. <see cref="ListByEntityAsync"/> backs this framework's own
/// Search section ("The framework should support searching by... Business Entity").
/// </summary>
public interface IDocumentAttachmentRepository
{
    Task<DocumentAttachment?> GetByIdAsync(DocumentAttachmentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentAttachment>> ListByEntityAsync(
        Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken);

    Task AddAsync(DocumentAttachment attachment, CancellationToken cancellationToken);
}
