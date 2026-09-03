using FluentAssertions;
using Hris.Foundation.Search.Domain;
using Xunit;

namespace Hris.Foundation.Search.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2: "Equality is by value, not
/// reference." These seven records are Domain Events, not Value Objects, but the same
/// expectation applies to any immutable data-carrying type this framework hands to a
/// caller -- the identical shape NumberingEventsTests already establishes.
/// </summary>
public sealed class SearchEventsTests
{
    [Fact]
    public void SearchRequested_HasValueEquality_AndAUsefulToString()
    {
        var original = new SearchRequested(Guid.NewGuid(), TestData.NowUtc, new SearchExecutionId(Guid.NewGuid()), TestData.TenantId, "Juan");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SearchRequested));
    }

    [Fact]
    public void SearchCompleted_HasValueEquality_AndAUsefulToString()
    {
        var original = new SearchCompleted(Guid.NewGuid(), TestData.NowUtc, new SearchExecutionId(Guid.NewGuid()), 5, 42);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SearchCompleted));
    }

    [Fact]
    public void SearchFailed_HasValueEquality_AndAUsefulToString()
    {
        var original = new SearchFailed(Guid.NewGuid(), TestData.NowUtc, new SearchExecutionId(Guid.NewGuid()), "Unknown domain");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SearchFailed));
    }

    [Fact]
    public void SearchIndexCreated_HasValueEquality_AndAUsefulToString()
    {
        var original = new SearchIndexCreated(
            Guid.NewGuid(), TestData.NowUtc, new IndexedDocumentId(Guid.NewGuid()), new SearchIndexDefinitionId(Guid.NewGuid()), TestData.TenantId);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SearchIndexCreated));
    }

    [Fact]
    public void SearchIndexUpdated_HasValueEquality_AndAUsefulToString()
    {
        var original = new SearchIndexUpdated(Guid.NewGuid(), TestData.NowUtc, new IndexedDocumentId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SearchIndexUpdated));
    }

    [Fact]
    public void SearchIndexRebuilt_HasValueEquality_AndAUsefulToString()
    {
        var original = new SearchIndexRebuilt(Guid.NewGuid(), TestData.NowUtc, new SearchIndexDefinitionId(Guid.NewGuid()), 1234);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SearchIndexRebuilt));
    }

    [Fact]
    public void SearchSuggestionGenerated_HasValueEquality_AndAUsefulToString()
    {
        var original = new SearchSuggestionGenerated(Guid.NewGuid(), TestData.NowUtc, new SavedSearchId(Guid.NewGuid()), TestData.UserId);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SearchSuggestionGenerated));
    }
}
