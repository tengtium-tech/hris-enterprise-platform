using FluentAssertions;
using Hris.Application.Pagination;
using Hris.Foundation.DocumentManagement.Application.Commands;
using Hris.Foundation.DocumentManagement.Application.Queries;
using Hris.Foundation.DocumentManagement.Application.Validators;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>FileStorageCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class DocumentManagementCommandValidatorsTests
{
    [Fact]
    public void CreateDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTitle()
    {
        var validator = new CreateDocumentCommandValidator();
        var valid = new CreateDocumentCommand(
            Guid.NewGuid(), "Title", "Category", "Confidential", Guid.NewGuid(), null, null, null, null, null, null, null);
        var invalid = valid with { Title = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddDocumentVersionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyStoredFileId()
    {
        var validator = new AddDocumentVersionCommandValidator();
        var valid = new AddDocumentVersionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, null, Guid.NewGuid());
        var invalid = valid with { StoredFileId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateDocumentMetadataCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCategory()
    {
        var validator = new UpdateDocumentMetadataCommandValidator();
        var valid = new UpdateDocumentMetadataCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Category", null, null, null, null, null, null);
        var invalid = valid with { Category = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReclassifyDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyClassification()
    {
        var validator = new ReclassifyDocumentCommandValidator();
        var valid = new ReclassifyDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "Restricted");
        var invalid = valid with { Classification = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReviewDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDocumentId()
    {
        var validator = new ReviewDocumentCommandValidator();

        validator.Validate(new ReviewDocumentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ReviewDocumentCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ApproveDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDocumentId()
    {
        var validator = new ApproveDocumentCommandValidator();

        validator.Validate(new ApproveDocumentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ApproveDocumentCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDocumentId()
    {
        var validator = new ActivateDocumentCommandValidator();

        validator.Validate(new ActivateDocumentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ActivateDocumentCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDocumentId()
    {
        var validator = new ArchiveDocumentCommandValidator();

        validator.Validate(new ArchiveDocumentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ArchiveDocumentCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DisposeDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDocumentId()
    {
        var validator = new DisposeDocumentCommandValidator();

        validator.Validate(new DisposeDocumentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new DisposeDocumentCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordDocumentDownloadCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDownloadedByUserId()
    {
        var validator = new RecordDocumentDownloadCommandValidator();
        var valid = new RecordDocumentDownloadCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { DownloadedByUserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AttachDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyEntityType()
    {
        var validator = new AttachDocumentCommandValidator();
        var valid = new AttachDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { EntityType = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DetachDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyAttachmentId()
    {
        var validator = new DetachDocumentCommandValidator();

        validator.Validate(new DetachDocumentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new DetachDocumentCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetDocumentQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyDocumentId()
    {
        var validator = new GetDocumentQueryValidator();

        validator.Validate(new GetDocumentQuery(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetDocumentQuery(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListDocumentAttachmentsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyEntityType()
    {
        var validator = new ListDocumentAttachmentsQueryValidator();
        var valid = new ListDocumentAttachmentsQuery(Guid.NewGuid(), "EmployeeProfile", Guid.NewGuid());
        var invalid = valid with { EntityType = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SearchDocumentsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new SearchDocumentsQueryValidator();
        var valid = new SearchDocumentsQuery(Guid.NewGuid(), null, null, null, null, new PageRequest(1, 20));
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
