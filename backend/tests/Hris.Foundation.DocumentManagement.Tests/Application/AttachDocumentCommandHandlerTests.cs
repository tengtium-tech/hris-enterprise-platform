using FluentAssertions;
using Hris.Foundation.DocumentManagement.Application.Commands;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class AttachDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _documentRepository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentAttachmentRepository _attachmentRepository = Substitute.For<IDocumentAttachmentRepository>();
    private readonly AttachDocumentCommandHandler _handler;

    public AttachDocumentCommandHandlerTests()
    {
        _handler = new AttachDocumentCommandHandler(_documentRepository, _attachmentRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheAttachment_WhenTheDocumentExistsForTheSameTenant()
    {
        var document = TestData.DraftDocument();
        _documentRepository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(
            new AttachDocumentCommand(document.Id.Value, TestData.TenantId, "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attachmentRepository.Received(1).AddAsync(Arg.Any<DocumentAttachment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist_WithoutCallingTheAttachmentRepository()
    {
        _documentRepository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(
            new AttachDocumentCommand(Guid.NewGuid(), TestData.TenantId, "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
        await _attachmentRepository.DidNotReceive().AddAsync(Arg.Any<DocumentAttachment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenEntityTypeIsEmpty_WithoutCallingTheAttachmentRepository()
    {
        var document = TestData.DraftDocument();
        _documentRepository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(
            new AttachDocumentCommand(document.Id.Value, TestData.TenantId, string.Empty, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.EntityTypeRequired);
        await _attachmentRepository.DidNotReceive().AddAsync(Arg.Any<DocumentAttachment>(), Arg.Any<CancellationToken>());
    }
}

public sealed class DetachDocumentCommandHandlerTests
{
    private readonly IDocumentAttachmentRepository _repository = Substitute.For<IDocumentAttachmentRepository>();
    private readonly DetachDocumentCommandHandler _handler;

    public DetachDocumentCommandHandlerTests()
    {
        _handler = new DetachDocumentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAttachmentExistsForTheSameTenant()
    {
        var attachment = DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), TestData.TenantId, "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid(), TestData.NowUtc).Value;
        _repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        var result = await _handler.Handle(
            new DetachDocumentCommand(attachment.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        attachment.Status.Should().Be(DocumentAttachmentStatus.Detached);
    }

    [Fact]
    public async Task Handle_Fails_WhenAttachmentBelongsToAnotherTenant()
    {
        var attachment = DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), Guid.NewGuid(), "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid(), TestData.NowUtc).Value;
        _repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        var result = await _handler.Handle(
            new DetachDocumentCommand(attachment.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentAttachmentNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenAlreadyDetached()
    {
        var attachment = DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), TestData.TenantId, "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid(), TestData.NowUtc).Value;
        attachment.Detach(TestData.NowUtc);
        _repository.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        var result = await _handler.Handle(new DetachDocumentCommand(attachment.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentAttachmentTransition);
    }
}
