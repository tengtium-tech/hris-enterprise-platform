using FluentAssertions;
using Hris.Foundation.Search.Domain;
using Xunit;

namespace Hris.Foundation.Search.Tests.Domain;

public sealed class SavedSearchTests
{
    [Fact]
    public void Save_Succeeds_AndRaisesNoEvent()
    {
        var result = SavedSearch.Save(TestData.TenantId, TestData.UserId, "My employees", "Engineer", "EMPLOYEE", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.OwnerUserId.Should().Be(TestData.UserId);
        result.Value.Name.Should().Be("My employees");
        result.Value.QueryText.Should().Be("Engineer");
        result.Value.DomainFilter.Should().Be("EMPLOYEE");
        result.Value.SuggestedCount.Should().Be(0);
        result.Value.DomainEvents.Should().BeEmpty("search-framework.md's own Domain Events list names no saved-search-created event");
    }

    [Fact]
    public void Save_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => SavedSearch.Save(Guid.Empty, TestData.UserId, "My employees", "Engineer", null, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Save_Throws_WhenOwnerUserIdIsEmpty()
    {
        var act = () => SavedSearch.Save(TestData.TenantId, Guid.Empty, "My employees", "Engineer", null, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_Fails_WhenNameIsMissing(string? name)
    {
        var result = SavedSearch.Save(TestData.TenantId, TestData.UserId, name, "Engineer", null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SavedSearchNameRequired);
    }

    [Fact]
    public void Save_Fails_WhenNameExceeds200Characters()
    {
        var result = SavedSearch.Save(TestData.TenantId, TestData.UserId, new string('a', 201), "Engineer", null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SavedSearchNameTooLong);
    }

    [Fact]
    public void Save_Fails_WhenQueryTextIsMissing()
    {
        var result = SavedSearch.Save(TestData.TenantId, TestData.UserId, "My employees", " ", null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.QueryTextRequired);
    }

    [Fact]
    public void Rename_Succeeds()
    {
        var savedSearch = TestData.SavedSearchFor();

        var result = savedSearch.Rename("Renamed");

        result.IsSuccess.Should().BeTrue();
        savedSearch.Name.Should().Be("Renamed");
    }

    [Fact]
    public void Rename_Fails_WhenNameIsMissing()
    {
        var savedSearch = TestData.SavedSearchFor();

        var result = savedSearch.Rename(" ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SavedSearchNameRequired);
    }

    [Fact]
    public void RecordSuggested_Succeeds_AndRaisesSearchSuggestionGenerated()
    {
        var savedSearch = TestData.SavedSearchFor();

        var result = savedSearch.RecordSuggested(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        savedSearch.SuggestedCount.Should().Be(1);
        savedSearch.LastSuggestedAtUtc.Should().Be(TestData.NowUtc);
        savedSearch.DomainEvents.OfType<SearchSuggestionGenerated>().Should().ContainSingle();
    }

    [Fact]
    public void RecordSuggested_Accumulates_AcrossMultipleCalls()
    {
        var savedSearch = TestData.SavedSearchFor();

        savedSearch.RecordSuggested(TestData.NowUtc);
        savedSearch.RecordSuggested(TestData.NowUtc.AddMinutes(1));

        savedSearch.SuggestedCount.Should().Be(2);
        savedSearch.DomainEvents.OfType<SearchSuggestionGenerated>().Should().HaveCount(2);
    }
}
