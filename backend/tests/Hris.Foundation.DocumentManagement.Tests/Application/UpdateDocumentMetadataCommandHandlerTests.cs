using FluentAssertions;
using Hris.Foundation.DocumentManagement.Application.Commands;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class UpdateDocumentMetadataCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly UpdateDocumentMetadataCommandHandler _handler;

    public UpdateDocumentMetadataCommandHandlerTests()
    {
        _handler = new UpdateDocumentMetadataCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndUpdatesTheDocument_WhenItExistsForTheSameTenant()
    {
        var document = TestData.DraftDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(
            new UpdateDocumentMetadataCommand(
                document.Id.Value, TestData.TenantId, "New Title", "PayrollDocuments", null, null, null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentBelongsToAnotherTenant()
    {
        var document = TestData.DraftDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(
            new UpdateDocumentMetadataCommand(
                document.Id.Value, Guid.NewGuid(), "New Title", "PayrollDocuments", null, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}

public sealed class ReclassifyDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly ReclassifyDocumentCommandHandler _handler;

    public ReclassifyDocumentCommandHandlerTests()
    {
        _handler = new ReclassifyDocumentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndChangesClassification()
    {
        var document = TestData.DraftDocument(classification: DocumentClassification.Internal);
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(
            new ReclassifyDocumentCommand(document.Id.Value, TestData.TenantId, "Restricted"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Classification.Should().Be(DocumentClassification.Restricted);
    }

    [Fact]
    public async Task Handle_Fails_WhenClassificationIsNotARecognizedValue_WithoutLoadingTheDocument()
    {
        var result = await _handler.Handle(
            new ReclassifyDocumentCommand(Guid.NewGuid(), TestData.TenantId, "NotARealClassification"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidClassification);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(
            new ReclassifyDocumentCommand(Guid.NewGuid(), TestData.TenantId, "Restricted"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}

public sealed class RecordDocumentDownloadCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly RecordDocumentDownloadCommandHandler _handler;

    public RecordDocumentDownloadCommandHandlerTests()
    {
        _handler = new RecordDocumentDownloadCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenACurrentVersionExists()
    {
        var document = TestData.UploadedDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);
        document.ClearDomainEvents();

        var result = await _handler.Handle(
            new RecordDocumentDownloadCommand(document.Id.Value, TestData.TenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.DomainEvents.Should().ContainSingle(e => e is DocumentDownloaded);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(
            new RecordDocumentDownloadCommand(Guid.NewGuid(), TestData.TenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}
