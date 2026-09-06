using FluentAssertions;
using Hris.Foundation.DocumentManagement.Application.Commands;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class CreateDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly CreateDocumentCommandHandler _handler;

    public CreateDocumentCommandHandlerTests()
    {
        _handler = new CreateDocumentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewDocument_WhenInputIsValid()
    {
        var command = new CreateDocumentCommand(
            TestData.TenantId, "Employment Contract", "EmployeeDocuments", "Confidential", TestData.OwnerUserId,
            Description: null, DocumentNumber: null, CompanyId: null, DepartmentId: null,
            EffectiveDate: null, ExpirationDate: null, Tags: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenClassificationIsNotARecognizedValue_WithoutCallingTheRepository()
    {
        var command = new CreateDocumentCommand(
            TestData.TenantId, "Employment Contract", "EmployeeDocuments", "NotARealClassification", TestData.OwnerUserId,
            null, null, null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidClassification);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTitleIsEmpty_WithoutCallingTheRepository()
    {
        var command = new CreateDocumentCommand(
            TestData.TenantId, string.Empty, "EmployeeDocuments", "Confidential", TestData.OwnerUserId,
            null, null, null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.TitleRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }
}
