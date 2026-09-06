using FluentAssertions;
using Hris.Foundation.DocumentManagement.Application.Commands;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class AddDocumentVersionCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly AddDocumentVersionCommandHandler _handler;

    public AddDocumentVersionCommandHandlerTests()
    {
        _handler = new AddDocumentVersionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDocumentExistsForTheSameTenant()
    {
        var document = TestData.DraftDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(
            new AddDocumentVersionCommand(document.Id.Value, TestData.TenantId, Guid.NewGuid(), true, null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Uploaded);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<DocumentId>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await _handler.Handle(
            new AddDocumentVersionCommand(Guid.NewGuid(), TestData.TenantId, Guid.NewGuid(), true, null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WithDocumentNotFound_NotAPermissionError_WhenDocumentBelongsToAnotherTenant()
    {
        var document = TestData.DraftDocument();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(
            new AddDocumentVersionCommand(document.Id.Value, Guid.NewGuid(), Guid.NewGuid(), true, null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.DocumentNotFound);
    }
}
