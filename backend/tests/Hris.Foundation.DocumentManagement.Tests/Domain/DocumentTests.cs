using FluentAssertions;
using Hris.Foundation.DocumentManagement.Domain;
using System.Linq;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Domain;

public sealed class DocumentTests
{
    [Fact]
    public void Create_Succeeds_AndStartsInDraft_WithNoVersion()
    {
        var result = Document.Create(
            TestData.TenantId, "Employment Contract", "EmployeeDocuments", DocumentClassification.Confidential,
            TestData.OwnerUserId, description: null, documentNumber: null, companyId: null, departmentId: null,
            effectiveDate: null, expirationDate: null, tags: null, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DocumentStatus.Draft);
        result.Value.CurrentVersion.Should().BeNull();
        var raised = result.Value.DomainEvents.Should().ContainSingle(e => e is DocumentCreated).Subject.Should().BeOfType<DocumentCreated>().Subject;
        raised.DocumentId.Should().Be(result.Value.Id);
        raised.TenantId.Should().Be(TestData.TenantId);
        raised.Category.Should().Be("EmployeeDocuments");
        raised.Classification.Should().Be(DocumentClassification.Confidential);
    }

    [Fact]
    public void Create_Fails_WhenTitleIsEmpty()
    {
        var result = Document.Create(
            TestData.TenantId, string.Empty, "EmployeeDocuments", DocumentClassification.Confidential,
            TestData.OwnerUserId, null, null, null, null, null, null, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.TitleRequired);
    }

    [Fact]
    public void Create_Fails_WhenCategoryIsEmpty()
    {
        var result = Document.Create(
            TestData.TenantId, "Employment Contract", string.Empty, DocumentClassification.Confidential,
            TestData.OwnerUserId, null, null, null, null, null, null, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.CategoryRequired);
    }

    [Fact]
    public void Create_Fails_WhenTenantIdIsEmpty()
    {
        var act = () => Document.Create(
            Guid.Empty, "Employment Contract", "EmployeeDocuments", DocumentClassification.Confidential,
            TestData.OwnerUserId, null, null, null, null, null, null, null, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Fails_WhenOwnerUserIdIsEmpty()
    {
        var act = () => Document.Create(
            TestData.TenantId, "Employment Contract", "EmployeeDocuments", DocumentClassification.Confidential,
            Guid.Empty, null, null, null, null, null, null, null, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddVersion_FirstCall_IsAlwaysMajorOneMinorZero_RegardlessOfIsMajorVersionFlag()
    {
        var document = TestData.DraftDocument();

        var result = document.AddVersion(Guid.NewGuid(), isMajorVersion: false, changeSummary: null, Guid.NewGuid(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.CurrentVersion.Should().NotBeNull();
        document.CurrentVersion!.MajorVersion.Should().Be(1);
        document.CurrentVersion.MinorVersion.Should().Be(0);
    }

    [Fact]
    public void AddVersion_OnAFreshDraft_TransitionsToUploaded_AndRaisesBothEvents()
    {
        var document = TestData.DraftDocument();
        var storedFileId = Guid.NewGuid();

        document.AddVersion(storedFileId, isMajorVersion: true, changeSummary: null, Guid.NewGuid(), TestData.NowUtc);

        document.Status.Should().Be(DocumentStatus.Uploaded);
        var versionCreated = document.DomainEvents.OfType<DocumentVersionCreated>().Should().ContainSingle().Subject;
        versionCreated.DocumentId.Should().Be(document.Id);
        versionCreated.DocumentVersionId.Should().Be(document.CurrentVersion!.Id);
        versionCreated.MajorVersion.Should().Be(1);
        var uploaded = document.DomainEvents.OfType<DocumentUploaded>().Should().ContainSingle().Subject;
        uploaded.DocumentId.Should().Be(document.Id);
        uploaded.DocumentVersionId.Should().Be(document.CurrentVersion.Id);
    }

    [Fact]
    public void AddVersion_MinorBump_IncrementsMinor_AndKeepsMajor()
    {
        var document = TestData.UploadedDocument();

        document.AddVersion(Guid.NewGuid(), isMajorVersion: false, changeSummary: "typo fix", Guid.NewGuid(), TestData.NowUtc);

        document.CurrentVersion!.MajorVersion.Should().Be(1);
        document.CurrentVersion.MinorVersion.Should().Be(1);
    }

    [Fact]
    public void AddVersion_MajorBump_IncrementsMajor_AndResetsMinorToZero()
    {
        var document = TestData.UploadedDocument();
        document.AddVersion(Guid.NewGuid(), isMajorVersion: false, changeSummary: null, Guid.NewGuid(), TestData.NowUtc);

        document.AddVersion(Guid.NewGuid(), isMajorVersion: true, changeSummary: null, Guid.NewGuid(), TestData.NowUtc);

        document.CurrentVersion!.MajorVersion.Should().Be(2);
        document.CurrentVersion.MinorVersion.Should().Be(0);
    }

    [Fact]
    public void AddVersion_SupersedesThePreviousVersion_WhichRemainsInHistory()
    {
        var document = TestData.UploadedDocument();
        var firstVersionId = document.CurrentVersion!.Id;

        document.AddVersion(Guid.NewGuid(), isMajorVersion: true, changeSummary: null, Guid.NewGuid(), TestData.NowUtc);

        document.Versions.Should().HaveCount(2);
        document.Versions.Single(v => v.Id == firstVersionId).Status.Should().Be(DocumentVersionStatus.Superseded);
        document.CurrentVersion!.Status.Should().Be(DocumentVersionStatus.Active);
    }

    [Fact]
    public void AddVersion_Fails_WhenDocumentIsDisposed()
    {
        var document = TestData.DisposedDocument();

        var result = document.AddVersion(Guid.NewGuid(), isMajorVersion: true, null, Guid.NewGuid(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void AddVersion_Fails_WhenStoredFileIdIsEmpty()
    {
        var document = TestData.DraftDocument();

        var result = document.AddVersion(Guid.Empty, isMajorVersion: true, null, Guid.NewGuid(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.StoredFileIdRequired);
    }

    [Fact]
    public void Review_Fails_WhenStillDraft()
    {
        var document = TestData.DraftDocument();

        var result = document.Review();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void Review_Fails_WhenAlreadyReviewed()
    {
        var document = TestData.ReviewedDocument();

        var result = document.Review();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void Review_Succeeds_FromUploaded_AndRaisesNoEvent()
    {
        var document = TestData.UploadedDocument();
        document.ClearDomainEvents();

        var result = document.Review();

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Reviewed);
        document.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Approve_Succeeds_FromReviewed_AndRaisesDocumentApproved()
    {
        var document = TestData.ReviewedDocument();
        document.ClearDomainEvents();

        var result = document.Approve(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Approved);
        var raised = document.DomainEvents.OfType<DocumentApproved>().Should().ContainSingle().Subject;
        raised.DocumentId.Should().Be(document.Id);
    }

    [Fact]
    public void Approve_Fails_WhenNotReviewed()
    {
        var document = TestData.UploadedDocument();

        var result = document.Approve(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void Activate_Succeeds_FromApproved_AndRaisesNoEvent()
    {
        var document = TestData.ApprovedDocument();
        document.ClearDomainEvents();

        var result = document.Activate();

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Active);
        document.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_Fails_WhenNotApproved()
    {
        var document = TestData.ReviewedDocument();

        var result = document.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void Archive_Succeeds_FromActive_AndRaisesDocumentArchived()
    {
        var document = TestData.ActiveDocument();
        document.ClearDomainEvents();

        var result = document.Archive(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Archived);
        var raised = document.DomainEvents.OfType<DocumentArchived>().Should().ContainSingle().Subject;
        raised.DocumentId.Should().Be(document.Id);
    }

    [Fact]
    public void Archive_Fails_WhenNotActive()
    {
        var document = TestData.ApprovedDocument();

        var result = document.Archive(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void MarkDisposed_Succeeds_FromArchived_AndRaisesDocumentDeleted()
    {
        var document = TestData.ArchivedDocument();
        document.ClearDomainEvents();

        var result = document.MarkDisposed(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Disposed);
        var raised = document.DomainEvents.OfType<DocumentDeleted>().Should().ContainSingle().Subject;
        raised.DocumentId.Should().Be(document.Id);
    }

    [Fact]
    public void MarkDisposed_Fails_WhenNotArchived()
    {
        var document = TestData.ActiveDocument();

        var result = document.MarkDisposed(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void RecordDownload_Succeeds_WhenACurrentVersionExists()
    {
        var document = TestData.UploadedDocument();
        var downloadedByUserId = Guid.NewGuid();
        document.ClearDomainEvents();

        var result = document.RecordDownload(downloadedByUserId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        var raised = document.DomainEvents.OfType<DocumentDownloaded>().Should().ContainSingle().Subject;
        raised.DocumentId.Should().Be(document.Id);
        raised.DocumentVersionId.Should().Be(document.CurrentVersion!.Id);
        raised.DownloadedByUserId.Should().Be(downloadedByUserId);
    }

    [Fact]
    public void RecordDownload_Fails_WhenNoVersionExistsYet()
    {
        var document = TestData.DraftDocument();

        var result = document.RecordDownload(Guid.NewGuid(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.NoCurrentVersion);
    }

    [Fact]
    public void UpdateMetadata_Succeeds_AndReplacesEveryField_AndRaisesMetadataUpdated()
    {
        var document = TestData.DraftDocument();
        document.ClearDomainEvents();
        var companyId = Guid.NewGuid();

        var result = document.UpdateMetadata(
            "Updated Title", "Updated description", "PayrollDocuments", ["urgent", "2026"],
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), companyId, null, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.Title.Should().Be("Updated Title");
        document.Category.Should().Be("PayrollDocuments");
        document.Tags.Should().BeEquivalentTo("urgent", "2026");
        document.CompanyId.Should().Be(companyId);
        var raised = document.DomainEvents.OfType<MetadataUpdated>().Should().ContainSingle().Subject;
        raised.DocumentId.Should().Be(document.Id);
    }

    [Fact]
    public void UpdateMetadata_Fails_WhenTitleIsEmpty()
    {
        var document = TestData.DraftDocument();

        var result = document.UpdateMetadata(string.Empty, null, "EmployeeDocuments", null, null, null, null, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.TitleRequired);
    }

    [Fact]
    public void UpdateMetadata_Fails_WhenCategoryIsEmpty()
    {
        var document = TestData.DraftDocument();

        var result = document.UpdateMetadata("New Title", null, string.Empty, null, null, null, null, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.CategoryRequired);
    }

    [Fact]
    public void UpdateMetadata_Fails_WhenDocumentIsDisposed()
    {
        var document = TestData.DisposedDocument();

        var result = document.UpdateMetadata("New Title", null, "EmployeeDocuments", null, null, null, null, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }

    [Fact]
    public void Reclassify_Succeeds_AndRaisesDocumentUpdated_NotMetadataUpdated()
    {
        var document = TestData.DraftDocument(classification: DocumentClassification.Internal);
        document.ClearDomainEvents();

        var result = document.Reclassify(DocumentClassification.Restricted, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.Classification.Should().Be(DocumentClassification.Restricted);
        var raised = document.DomainEvents.OfType<DocumentUpdated>().Should().ContainSingle().Subject;
        raised.DocumentId.Should().Be(document.Id);
    }

    [Fact]
    public void Reclassify_Fails_WhenDocumentIsDisposed()
    {
        var document = TestData.DisposedDocument();

        var result = document.Reclassify(DocumentClassification.Public, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentLifecycleTransition);
    }
}
