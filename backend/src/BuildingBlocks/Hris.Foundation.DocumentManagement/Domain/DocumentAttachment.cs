using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Aggregate Root associating one <see cref="Document"/> with one business entity.
/// Source: docs/03-foundation/document-management.md, Attachment ("Attachments
/// associate documents with business entities... A document may be attached to
/// multiple entities where permitted").
///
/// A separate Aggregate Root, never a child collection of <see cref="Document"/>
/// itself -- a single company policy document could legitimately be attached to every
/// employee profile in a tenant, an effectively unbounded collection from the
/// document's own side, the same "unbounded collection, own aggregate" reasoning
/// aggregate-design-rules.md's own sizing guidance already applies elsewhere in this
/// codebase to a many-side relationship. <see cref="DocumentId"/> is a reference by
/// identifier only (aggregate-design-rules.md Rule 5: "Reference other Aggregates by
/// Identity, not by object").
///
/// <see cref="EntityType"/> is a validated, non-empty string rather than a closed enum
/// -- the source document's own examples (Employee Profile, Leave Request, Payroll,
/// Recruitment Application, Performance Review, Training Record) are business-module
/// concepts this Sprint's own Foundation-layer build does not yet define; the same
/// "business modules invent their own values in Phase 2+" reasoning already governs
/// <c>CacheKey.Region</c> and <c>PermissionKey.ResourceType</c> in this codebase.
/// </summary>
public sealed class DocumentAttachment : AggregateRoot<DocumentAttachmentId>
{
    public DocumentId DocumentId { get; }

    public Guid TenantId { get; }

    public string EntityType { get; }

    public Guid EntityId { get; }

    public Guid AttachedByUserId { get; }

    public DateTimeOffset AttachedAtUtc { get; }

    public DocumentAttachmentStatus Status { get; private set; }

    public DateTimeOffset? DetachedAtUtc { get; private set; }

    private DocumentAttachment(
        DocumentAttachmentId id,
        DocumentId documentId,
        Guid tenantId,
        string entityType,
        Guid entityId,
        Guid attachedByUserId,
        DateTimeOffset attachedAtUtc)
        : base(id)
    {
        DocumentId = documentId;
        TenantId = tenantId;
        EntityType = entityType;
        EntityId = entityId;
        AttachedByUserId = attachedByUserId;
        AttachedAtUtc = attachedAtUtc;
        Status = DocumentAttachmentStatus.Attached;
    }

    /// <summary>
    /// Raises <see cref="DocumentAttached"/>. Does not itself verify
    /// <paramref name="documentId"/> resolves to a real, same-tenant
    /// <see cref="Document"/> -- the calling command handler does that first (loading
    /// the <see cref="Document"/> and checking its own <see cref="Document.TenantId"/>),
    /// the same "verify the referenced aggregate at the Application layer, before
    /// calling a Domain factory that only knows the identifier" split every other
    /// cross-aggregate reference in this codebase already follows.
    /// </summary>
    public static Result<DocumentAttachment> Attach(
        DocumentId documentId,
        Guid tenantId,
        string? entityType,
        Guid entityId,
        Guid attachedByUserId,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstDefault(entityId, nameof(entityId));
        Guard.AgainstDefault(attachedByUserId, nameof(attachedByUserId));

        if (string.IsNullOrWhiteSpace(entityType))
        {
            return Result.Failure<DocumentAttachment>(DocumentErrors.EntityTypeRequired);
        }

        var attachment = new DocumentAttachment(
            new DocumentAttachmentId(Guid.NewGuid()), documentId, tenantId, entityType.Trim(), entityId, attachedByUserId, nowUtc);

        attachment.AddDomainEvent(new DocumentAttached(
            Guid.NewGuid(), nowUtc, attachment.Id, documentId, tenantId, attachment.EntityType, entityId));

        return Result.Success(attachment);
    }

    /// <summary>
    /// A soft detach -- the row, and its own attach/detach history, remains for audit
    /// purposes rather than being deleted. Raises no event: the source document names
    /// none for removing an attachment, only for creating one.
    /// </summary>
    public Result Detach(DateTimeOffset nowUtc)
    {
        if (Status != DocumentAttachmentStatus.Attached)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentAttachmentTransition);
        }

        Status = DocumentAttachmentStatus.Detached;
        DetachedAtUtc = nowUtc;
        return Result.Success();
    }
}
