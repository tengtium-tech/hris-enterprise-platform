using FluentAssertions;
using Hris.Foundation.DocumentManagement.Application.Queries;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class GetDocumentQueryHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly GetDocumentQueryHandler _handler;

    public GetDocumentQueryHandlerTests()
    {
        _handler = new GetDocumentQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDocumentExistsForTheSameTenant()
    {
        var document = TestData.DraftDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new GetDocumentQuery(document.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.DocumentId.Should().Be(document.Id.Value);
        dto.TenantId.Should().Be(document.TenantId);
        dto.Title.Should().Be(document.Title);
        dto.Category.Should().Be(document.Category);
        dto.Classification.Should().Be(document.Classification.ToString());
        dto.OwnerUserId.Should().Be(document.OwnerUserId);
        dto.CreatedAtUtc.Should().Be(document.CreatedAtUtc);
        dto.Status.Should().Be(document.Status.ToString());
        dto.CurrentVersion.Should().BeNull();
        dto.VersionCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Succeeds_AndMapsTheCurrentVersion_WhenOneExists()
    {
        var document = TestData.UploadedDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new GetDocumentQuery(document.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var version = result.Value.CurrentVersion;
        version.Should().NotBeNull();
        version!.DocumentVersionId.Should().Be(document.CurrentVersion!.Id.Value);
        version.MajorVersion.Should().Be(document.CurrentVersion.MajorVersion);
        version.MinorVersion.Should().Be(document.CurrentVersion.MinorVersion);
        version.StoredFileId.Should().Be(document.CurrentVersion.StoredFileId);
        version.CreatedByUserId.Should().Be(document.CurrentVersion.CreatedByUserId);
        version.Status.Should().Be(document.CurrentVersion.Status.ToString());
        result.Value.VersionCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(new GetDocumentQuery(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }

    /// <summary>
    /// CTR-ISO-002: "Requesting a record by an identifier belonging to another tenant
    /// must return not-found, not the record and not a permission error that confirms
    /// the record's existence." This is the one test in this project verifying that
    /// contract directly at the Application layer, per document-management.md's own
    /// unusually explicit isolation requirement.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsDocumentNotFound_NeverThePermissionErrorItWouldConfirmExistence_ForACrossTenantRequest()
    {
        var document = TestData.DraftDocument(tenantId: Guid.NewGuid());
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new GetDocumentQuery(document.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}
