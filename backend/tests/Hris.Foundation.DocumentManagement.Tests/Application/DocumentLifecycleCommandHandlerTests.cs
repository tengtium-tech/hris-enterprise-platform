using FluentAssertions;
using Hris.Foundation.DocumentManagement.Application.Commands;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class ReviewDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly ReviewDocumentCommandHandler _handler;

    public ReviewDocumentCommandHandlerTests()
    {
        _handler = new ReviewDocumentCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenUploaded()
    {
        var document = TestData.UploadedDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new ReviewDocumentCommand(document.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Reviewed);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(new ReviewDocumentCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}

public sealed class ApproveDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly ApproveDocumentCommandHandler _handler;

    public ApproveDocumentCommandHandlerTests()
    {
        _handler = new ApproveDocumentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenReviewed()
    {
        var document = TestData.ReviewedDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new ApproveDocumentCommand(document.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Approved);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(new ApproveDocumentCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}

public sealed class ActivateDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly ActivateDocumentCommandHandler _handler;

    public ActivateDocumentCommandHandlerTests()
    {
        _handler = new ActivateDocumentCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenApproved()
    {
        var document = TestData.ApprovedDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new ActivateDocumentCommand(document.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(new ActivateDocumentCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}

public sealed class ArchiveDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly ArchiveDocumentCommandHandler _handler;

    public ArchiveDocumentCommandHandlerTests()
    {
        _handler = new ArchiveDocumentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenActive()
    {
        var document = TestData.ActiveDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new ArchiveDocumentCommand(document.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Archived);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(new ArchiveDocumentCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}

public sealed class DisposeDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly DisposeDocumentCommandHandler _handler;

    public DisposeDocumentCommandHandlerTests()
    {
        _handler = new DisposeDocumentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenArchived()
    {
        var document = TestData.ArchivedDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new DisposeDocumentCommand(document.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Disposed);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(new DisposeDocumentCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}
