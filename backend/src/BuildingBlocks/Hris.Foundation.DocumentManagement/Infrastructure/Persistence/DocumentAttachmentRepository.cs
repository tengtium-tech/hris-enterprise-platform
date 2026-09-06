using Hris.Foundation.DocumentManagement.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.DocumentManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IDocumentAttachmentRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class DocumentAttachmentRepository : IDocumentAttachmentRepository
{
    private readonly HrisDbContext _dbContext;

    public DocumentAttachmentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<DocumentAttachment?> GetByIdAsync(DocumentAttachmentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<DocumentAttachment>().FirstOrDefaultAsync(attachment => attachment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DocumentAttachment>> ListByEntityAsync(
        Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        Guard.AgainstNullOrWhiteSpace(entityType, nameof(entityType));

        return await _dbContext.Set<DocumentAttachment>()
            .Where(attachment =>
                attachment.TenantId == tenantId
                && attachment.EntityType == entityType
                && attachment.EntityId == entityId
                && attachment.Status == DocumentAttachmentStatus.Attached)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(DocumentAttachment attachment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(attachment, nameof(attachment));
        await _dbContext.Set<DocumentAttachment>().AddAsync(attachment, cancellationToken).ConfigureAwait(false);
    }
}
