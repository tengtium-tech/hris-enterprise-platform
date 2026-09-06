namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Source: docs/03-foundation/document-management.md, Attachment ("A document may be
/// attached to multiple entities where permitted"). <see cref="Detached"/> is a soft
/// state, never a row deletion -- an attachment's own history (who attached this
/// document to this business entity, and when it was later detached) is itself part
/// of this framework's own required audit trail (Security Considerations: "Every
/// document access should be auditable").
/// </summary>
public enum DocumentAttachmentStatus
{
    Attached = 0,
    Detached,
}
