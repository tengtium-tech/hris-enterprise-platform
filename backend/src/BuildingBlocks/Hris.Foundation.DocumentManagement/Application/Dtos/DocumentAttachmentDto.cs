namespace Hris.Foundation.DocumentManagement.Application.Dtos;

/// <summary>
/// The read-side shape <c>ListDocumentAttachmentsQuery</c> returns, per
/// dto-design.md's own convention.
/// </summary>
public sealed record DocumentAttachmentDto(
    Guid DocumentAttachmentId,
    Guid DocumentId,
    string EntityType,
    Guid EntityId,
    Guid AttachedByUserId,
    DateTimeOffset AttachedAtUtc,
    string Status,
    DateTimeOffset? DetachedAtUtc);
