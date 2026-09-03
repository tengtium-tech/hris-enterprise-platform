using FluentAssertions;
using Hris.Foundation.Search.Application.Commands;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class SavedSearchCommandHandlerTests
{
    private readonly ISavedSearchRepository _repository = Substitute.For<ISavedSearchRepository>();

    [Fact]
    public async Task SaveSearch_Succeeds_AndPersistsTheNewSavedSearch()
    {
        var handler = new SaveSearchCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(
            new SaveSearchCommand(TestData.TenantId, TestData.UserId, "My employees", "Engineer", "EMPLOYEE"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<SavedSearch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveSearch_Fails_WhenNameIsMissing_WithoutCallingTheRepository()
    {
        var handler = new SaveSearchCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(
            new SaveSearchCommand(TestData.TenantId, TestData.UserId, string.Empty, "Engineer", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SavedSearchNameRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<SavedSearch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenameSavedSearch_Succeeds_WhenSavedSearchExists()
    {
        var savedSearch = TestData.SavedSearchFor();
        _repository.GetByIdAsync(savedSearch.Id, Arg.Any<CancellationToken>()).Returns(savedSearch);
        var handler = new RenameSavedSearchCommandHandler(_repository);

        var result = await handler.Handle(new RenameSavedSearchCommand(savedSearch.Id.Value, "Renamed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        savedSearch.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task RenameSavedSearch_Fails_WhenSavedSearchDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<SavedSearchId>(), Arg.Any<CancellationToken>()).Returns((SavedSearch?)null);
        var handler = new RenameSavedSearchCommandHandler(_repository);

        var result = await handler.Handle(new RenameSavedSearchCommand(Guid.NewGuid(), "Renamed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SavedSearchNotFound);
    }

    [Fact]
    public async Task DeleteSavedSearch_Succeeds_WhenSavedSearchExists()
    {
        var savedSearch = TestData.SavedSearchFor();
        _repository.GetByIdAsync(savedSearch.Id, Arg.Any<CancellationToken>()).Returns(savedSearch);
        var handler = new DeleteSavedSearchCommandHandler(_repository);

        var result = await handler.Handle(new DeleteSavedSearchCommand(savedSearch.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).DeleteAsync(savedSearch, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSavedSearch_Fails_WhenSavedSearchDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<SavedSearchId>(), Arg.Any<CancellationToken>()).Returns((SavedSearch?)null);
        var handler = new DeleteSavedSearchCommandHandler(_repository);

        var result = await handler.Handle(new DeleteSavedSearchCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SavedSearchNotFound);
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<SavedSearch>(), Arg.Any<CancellationToken>());
    }
}
