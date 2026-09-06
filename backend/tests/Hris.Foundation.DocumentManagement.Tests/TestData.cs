using Hris.Foundation.DocumentManagement.Domain;

namespace Hris.Foundation.DocumentManagement.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid TenantId = Guid.NewGuid();

    public static readonly Guid OwnerUserId = Guid.NewGuid();

    /// <summary>A newly registered document, in <see cref="DocumentStatus.Draft"/>.</summary>
    public static Document DraftDocument(
        Guid? tenantId = null,
        string title = "Employment Contract",
        string category = "EmployeeDocuments",
        DocumentClassification classification = DocumentClassification.Confidential,
        Guid? ownerUserId = null,
        DateTimeOffset? nowUtc = null) =>
        Document.Create(
            tenantId ?? TenantId,
            title,
            category,
            classification,
            ownerUserId ?? OwnerUserId,
            description: null,
            documentNumber: null,
            companyId: null,
            departmentId: null,
            effectiveDate: null,
            expirationDate: null,
            tags: null,
            nowUtc ?? NowUtc).Value;

    /// <summary>A document with one recorded version, in <see cref="DocumentStatus.Uploaded"/>.</summary>
    public static Document UploadedDocument(DateTimeOffset? nowUtc = null)
    {
        var document = DraftDocument(nowUtc: nowUtc);
        document.AddVersion(Guid.NewGuid(), isMajorVersion: true, changeSummary: null, Guid.NewGuid(), nowUtc ?? NowUtc);
        return document;
    }

    /// <summary>A document walked through to <see cref="DocumentStatus.Reviewed"/>.</summary>
    public static Document ReviewedDocument(DateTimeOffset? nowUtc = null)
    {
        var document = UploadedDocument(nowUtc);
        document.Review();
        return document;
    }

    /// <summary>A document walked through to <see cref="DocumentStatus.Approved"/>.</summary>
    public static Document ApprovedDocument(DateTimeOffset? nowUtc = null)
    {
        var document = ReviewedDocument(nowUtc);
        document.Approve(nowUtc ?? NowUtc);
        return document;
    }

    /// <summary>A document walked through to <see cref="DocumentStatus.Active"/>.</summary>
    public static Document ActiveDocument(DateTimeOffset? nowUtc = null)
    {
        var document = ApprovedDocument(nowUtc);
        document.Activate();
        return document;
    }

    /// <summary>A document walked through to <see cref="DocumentStatus.Archived"/>.</summary>
    public static Document ArchivedDocument(DateTimeOffset? nowUtc = null)
    {
        var document = ActiveDocument(nowUtc);
        document.Archive(nowUtc ?? NowUtc);
        return document;
    }

    /// <summary>A document walked through to <see cref="DocumentStatus.Disposed"/>.</summary>
    public static Document DisposedDocument(DateTimeOffset? nowUtc = null)
    {
        var document = ArchivedDocument(nowUtc);
        document.MarkDisposed(nowUtc ?? NowUtc);
        return document;
    }
}
