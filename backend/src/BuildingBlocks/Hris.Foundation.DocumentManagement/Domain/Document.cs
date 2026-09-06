using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Aggregate Root of the Document Management Framework's own business-document
/// abstraction. Source: docs/03-foundation/document-management.md, Core Concepts
/// ("A Document represents a managed business artifact") and Document Lifecycle.
///
/// Deliberately excludes physical file bytes, checksums, and storage-provider
/// concerns -- this document's own Scope section states plainly "Physical storage is
/// provided by the File Storage Framework," confirmed from that framework's own side
/// (<c>StoredFile</c>'s own remarks: "Business metadata belongs to the Document
/// Management Framework"). <see cref="DocumentVersion.StoredFileId"/> is this
/// aggregate's only reference to that physical layer, and it is a plain caller-supplied
/// <see cref="Guid"/>, not a cross-project dependency -- see that type's own remarks.
///
/// <see cref="TenantId"/> is a plain, caller-supplied <see cref="Guid"/>, the same
/// "built concretely" choice this codebase's own tenant-scoped aggregates (for example
/// <c>Notification</c>, <c>WorkflowInstance</c>, <c>Job</c>) already make for
/// themselves. This document's own AI Implementation Guidance is unusually explicit
/// about isolation ("Scope every document to a tenant and enforce isolation on
/// retrieval, including direct identifier access, CTR-ISO-001, CTR-ISO-002") --
/// enforced in this framework's own Application-layer query/command handlers, which
/// verify a loaded aggregate's own <see cref="TenantId"/> against the caller's tenant
/// context before returning anything, per CTR-ISO-002's own "not-found, not... a
/// permission error that confirms the record's existence."
/// </summary>
public sealed class Document : AggregateRoot<DocumentId>
{
    private readonly List<DocumentVersion> _versions = [];

    public Guid TenantId { get; }

    public string? DocumentNumber { get; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public string Category { get; private set; }

    public DocumentClassification Classification { get; private set; }

    public Guid OwnerUserId { get; }

    /// <summary>
    /// Metadata's own "Created Date" -- also this aggregate's own natural, stable
    /// ordering key for <c>SearchDocumentsQuery</c>'s own paging, since a document has
    /// no other single always-populated timestamp field of its own (unlike
    /// <c>Notification.RequestedAtUtc</c>, there is no later-arriving event this
    /// value could be confused with).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; }

    public Guid? CompanyId { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public DateOnly? EffectiveDate { get; private set; }

    public DateOnly? ExpirationDate { get; private set; }

    /// <summary>
    /// A private-setter auto-property, not a manually-managed backing field with
    /// <c>AddRange</c>/<c>Clear</c> -- unlike <see cref="Versions"/> (an owned-entity
    /// collection EF Core maps via <c>OwnsMany</c> against a field), this is a scalar
    /// value the Infrastructure layer's own EF Core configuration converts to and from
    /// a single delimited column (see <c>DocumentConfiguration</c>'s own remarks); a
    /// private setter is the standard, directly-supported EF Core shape for that,
    /// with no field-type mismatch to work around.
    /// </summary>
    public IReadOnlyList<string> Tags { get; private set; } = [];

    public DocumentStatus Status { get; private set; }

    public IReadOnlyList<DocumentVersion> Versions => _versions.AsReadOnly();

    /// <summary>
    /// The one version currently in force -- deliberately not its own stored field,
    /// the same "computed from the last appended element" choice
    /// <c>StoredFile.CurrentVersion</c> already establishes for the identical shape.
    /// </summary>
    public DocumentVersion? CurrentVersion => _versions.Count == 0 ? null : _versions[^1];

    private Document(
        DocumentId id,
        Guid tenantId,
        string? documentNumber,
        string title,
        string? description,
        string category,
        DocumentClassification classification,
        Guid ownerUserId,
        DateTimeOffset createdAtUtc,
        Guid? companyId,
        Guid? departmentId,
        DateOnly? effectiveDate,
        DateOnly? expirationDate,
        IReadOnlyList<string>? tags)
        : base(id)
    {
        TenantId = tenantId;
        DocumentNumber = documentNumber;
        Title = title;
        Description = description;
        Category = category;
        Classification = classification;
        OwnerUserId = ownerUserId;
        CreatedAtUtc = createdAtUtc;
        CompanyId = companyId;
        DepartmentId = departmentId;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        Status = DocumentStatus.Draft;
        Tags = tags is null ? [] : tags.ToList();
    }

    /// <summary>
    /// Registers a new document record, in <see cref="DocumentStatus.Draft"/> -- no
    /// version exists yet; see <see cref="AddVersion"/> for the transition to
    /// <see cref="DocumentStatus.Uploaded"/>. Raises <see cref="DocumentCreated"/>.
    /// </summary>
    public static Result<Document> Create(
        Guid tenantId,
        string? title,
        string? category,
        DocumentClassification classification,
        Guid ownerUserId,
        string? description,
        string? documentNumber,
        Guid? companyId,
        Guid? departmentId,
        DateOnly? effectiveDate,
        DateOnly? expirationDate,
        IReadOnlyList<string>? tags,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstDefault(ownerUserId, nameof(ownerUserId));

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Document>(DocumentErrors.TitleRequired);
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > 260)
        {
            return Result.Failure<Document>(DocumentErrors.TitleTooLong);
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure<Document>(DocumentErrors.CategoryRequired);
        }

        var document = new Document(
            new DocumentId(Guid.NewGuid()),
            tenantId,
            string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber.Trim(),
            trimmedTitle,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            category.Trim(),
            classification,
            ownerUserId,
            nowUtc,
            companyId,
            departmentId,
            effectiveDate,
            expirationDate,
            tags);

        document.AddDomainEvent(new DocumentCreated(Guid.NewGuid(), nowUtc, document.Id, tenantId, document.Category, classification));

        return Result.Success(document);
    }

    /// <summary>
    /// Records a new business-level version, backed by an already-uploaded physical
    /// file. The very first version is always Major 1, Minor 0, regardless of
    /// <paramref name="isMajorVersion"/> -- there is no prior version for a minor bump
    /// to be relative to. A version recorded while <see cref="Status"/> is still
    /// <see cref="DocumentStatus.Draft"/> also advances it to
    /// <see cref="DocumentStatus.Uploaded"/> and raises <see cref="DocumentUploaded"/>,
    /// in addition to the <see cref="DocumentVersionCreated"/> every call raises; a
    /// later version recorded against an already-<see cref="DocumentStatus.Active"/>
    /// document does not restart the review/approval lifecycle -- Document Management
    /// framework's own source document does not state that superseding content forces
    /// re-review, unlike, for example, a workflow-gated approval step would.
    /// </summary>
    public Result<Guid> AddVersion(Guid storedFileId, bool isMajorVersion, string? changeSummary, Guid createdByUserId, DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(createdByUserId, nameof(createdByUserId));

        if (Status == DocumentStatus.Disposed)
        {
            return Result.Failure<Guid>(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        if (storedFileId == Guid.Empty)
        {
            return Result.Failure<Guid>(DocumentErrors.StoredFileIdRequired);
        }

        var previousVersion = CurrentVersion;
        previousVersion?.Supersede();

        var (majorVersion, minorVersion) = previousVersion is null
            ? (1, 0)
            : isMajorVersion
                ? (previousVersion.MajorVersion + 1, 0)
                : (previousVersion.MajorVersion, previousVersion.MinorVersion + 1);

        var version = new DocumentVersion(
            new DocumentVersionId(Guid.NewGuid()),
            majorVersion,
            minorVersion,
            storedFileId,
            createdByUserId,
            nowUtc,
            string.IsNullOrWhiteSpace(changeSummary) ? null : changeSummary.Trim());

        _versions.Add(version);

        var wasDraft = Status == DocumentStatus.Draft;
        if (wasDraft)
        {
            Status = DocumentStatus.Uploaded;
        }

        AddDomainEvent(new DocumentVersionCreated(Guid.NewGuid(), nowUtc, Id, version.Id, majorVersion, minorVersion));
        if (wasDraft)
        {
            AddDomainEvent(new DocumentUploaded(Guid.NewGuid(), nowUtc, Id, version.Id));
        }

        return Result.Success(version.Id.Value);
    }

    /// <summary>
    /// Raises no event of its own -- this document's own Domain Events section names
    /// none for the Uploaded-to-Reviewed transition, the same asymmetric-event-list
    /// pattern this framework's own <see cref="DocumentEvents"/> file explains.
    /// </summary>
    public Result Review()
    {
        if (Status != DocumentStatus.Uploaded)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        Status = DocumentStatus.Reviewed;
        return Result.Success();
    }

    public Result Approve(DateTimeOffset nowUtc)
    {
        if (Status != DocumentStatus.Reviewed)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        Status = DocumentStatus.Approved;
        AddDomainEvent(new DocumentApproved(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Raises no event of its own -- see <see cref="Review"/>'s own remarks; the
    /// source document names no event for Approved-to-Active either.
    /// </summary>
    public Result Activate()
    {
        if (Status != DocumentStatus.Approved)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        Status = DocumentStatus.Active;
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status != DocumentStatus.Active)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        Status = DocumentStatus.Archived;
        AddDomainEvent(new DocumentArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// The terminal transition -- source document's own lifecycle stage is named
    /// "Disposed," but its own Domain Events section names the corresponding event
    /// <see cref="DocumentDeleted"/>, not "DocumentDisposed"; this method is named
    /// <c>MarkDisposed</c> rather than <c>Dispose</c> to avoid colliding with the
    /// <see cref="IDisposable"/> naming convention .NET analyzers (CA1063, CA2215)
    /// expect from a parameterless <c>Dispose()</c> method, even though this type
    /// implements no such interface.
    /// </summary>
    public Result MarkDisposed(DateTimeOffset nowUtc)
    {
        if (Status != DocumentStatus.Archived)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        Status = DocumentStatus.Disposed;
        AddDomainEvent(new DocumentDeleted(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Audit-only, per Security Considerations ("Every document access should be
    /// auditable") -- never changes <see cref="Status"/>.
    /// </summary>
    public Result RecordDownload(Guid downloadedByUserId, DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(downloadedByUserId, nameof(downloadedByUserId));

        if (CurrentVersion is null)
        {
            return Result.Failure(DocumentErrors.NoCurrentVersion);
        }

        AddDomainEvent(new DocumentDownloaded(Guid.NewGuid(), nowUtc, Id, CurrentVersion.Id, downloadedByUserId));
        return Result.Success();
    }

    /// <summary>
    /// Updates the Metadata-section fields (Metadata Management: "Title, Description,
    /// Category... Company, Department... Effective Date, Expiration Date, Tags") as
    /// one unit, raising <see cref="MetadataUpdated"/> once -- distinct from
    /// <see cref="Reclassify"/>, which this source document's own Core Concepts section
    /// treats as a separate concept. Rejected once <see cref="Status"/> reaches
    /// <see cref="DocumentStatus.Disposed"/> -- a disposed document's own metadata is
    /// historical record from that point on.
    /// </summary>
    public Result UpdateMetadata(
        string? title,
        string? description,
        string? category,
        IReadOnlyList<string>? tags,
        DateOnly? effectiveDate,
        DateOnly? expirationDate,
        Guid? companyId,
        Guid? departmentId,
        DateTimeOffset nowUtc)
    {
        if (Status == DocumentStatus.Disposed)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(DocumentErrors.TitleRequired);
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > 260)
        {
            return Result.Failure(DocumentErrors.TitleTooLong);
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure(DocumentErrors.CategoryRequired);
        }

        Title = trimmedTitle;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Category = category.Trim();
        CompanyId = companyId;
        DepartmentId = departmentId;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        Tags = tags is null ? [] : tags.ToList();

        AddDomainEvent(new MetadataUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Changes <see cref="Classification"/> alone. Raises <see cref="DocumentUpdated"/>
    /// -- see this framework's own <see cref="DocumentEvents"/> file for why this,
    /// rather than <see cref="MetadataUpdated"/>, is the event this transition raises.
    /// </summary>
    public Result Reclassify(DocumentClassification newClassification, DateTimeOffset nowUtc)
    {
        if (Status == DocumentStatus.Disposed)
        {
            return Result.Failure(DocumentErrors.InvalidDocumentLifecycleTransition);
        }

        Classification = newClassification;
        AddDomainEvent(new DocumentUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}
