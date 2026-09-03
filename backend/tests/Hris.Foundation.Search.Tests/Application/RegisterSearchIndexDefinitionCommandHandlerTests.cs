using FluentAssertions;
using Hris.Foundation.Search.Application.Commands;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class RegisterSearchIndexDefinitionCommandHandlerTests
{
    private readonly ISearchIndexDefinitionRepository _repository = Substitute.For<ISearchIndexDefinitionRepository>();
    private readonly RegisterSearchIndexDefinitionCommandHandler _handler;

    public RegisterSearchIndexDefinitionCommandHandlerTests()
    {
        _handler = new RegisterSearchIndexDefinitionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static RegisterSearchIndexDefinitionCommand ValidCommand(string entityType = "Employee") =>
        new(entityType, TestData.NewFields(), "employee.read");

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewDefinition_WhenEntityTypeIsAvailable()
    {
        _repository.ExistsByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<SearchIndexDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenEntityTypeIsAlreadyRegistered()
    {
        _repository.ExistsByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SearchIndexDefinitionAlreadyRegistered);
        await _repository.DidNotReceive().AddAsync(Arg.Any<SearchIndexDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenEntityTypeIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand(entityType: string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.EntityTypeRequired);
        await _repository.DidNotReceive().ExistsByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenFieldsAreInvalid()
    {
        _repository.ExistsByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = ValidCommand() with { Fields = [] };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.NoFieldsProvided);
    }
}
