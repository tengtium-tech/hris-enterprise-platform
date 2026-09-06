using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// document-management.md's own Domain Events section names exactly ten events --
/// every one implemented here, split nine/one across <see cref="Document"/> and
/// <see cref="DocumentAttachment"/>. Not every <see cref="DocumentStatus"/> transition
/// raises one: the document names no event for Reviewed or Active, and
/// <see cref="DocumentAttachment"/>'s own Detach transition raises none either -- the
/// same asymmetric-event-list pattern every other framework in this codebase already
/// follows where a source document's own named list is shorter than its own state
/// diagram (see, for example, <c>Notification</c>'s own remarks on Scheduled,
/// Processing, Acknowledged, Expired, Suppressed, and DeadLetter).
///
/// The source document's own event is named <c>DocumentDeleted</c>, not
/// <c>DocumentDisposed</c>, even though the lifecycle stage it corresponds to is
/// "Disposed" -- the event name here matches the document's own Domain Events section
/// literally rather than the lifecycle diagram's own stage name.
/// </summary>
public sealed record DocumentCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId,
    Guid TenantId,
    string Category,
    DocumentClassification Classification) : IDomainEvent;

public sealed record DocumentUploaded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId,
    DocumentVersionId DocumentVersionId) : IDomainEvent;

public sealed record DocumentVersionCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId,
    DocumentVersionId DocumentVersionId,
    int MajorVersion,
    int MinorVersion) : IDomainEvent;

public sealed record DocumentApproved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId) : IDomainEvent;

public sealed record DocumentArchived(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId) : IDomainEvent;

public sealed record DocumentDeleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId) : IDomainEvent;

public sealed record DocumentDownloaded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId,
    DocumentVersionId DocumentVersionId,
    Guid DownloadedByUserId) : IDomainEvent;

public sealed record MetadataUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId) : IDomainEvent;

/// <summary>
/// Raised by <see cref="Document.Reclassify"/> -- the source document's own Core
/// Concepts section treats Classification as distinct from Metadata (they are two
/// separate named subsections), so a classification change raises this generic event
/// rather than <see cref="MetadataUpdated"/>.
/// </summary>
public sealed record DocumentUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentId DocumentId) : IDomainEvent;

public sealed record DocumentAttached(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    DocumentAttachmentId DocumentAttachmentId,
    DocumentId DocumentId,
    Guid TenantId,
    string EntityType,
    Guid EntityId) : IDomainEvent;
