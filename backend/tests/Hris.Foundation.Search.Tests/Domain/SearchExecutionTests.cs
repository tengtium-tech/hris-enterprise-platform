using FluentAssertions;
using Hris.Foundation.Search.Domain;
using Xunit;

namespace Hris.Foundation.Search.Tests.Domain;

public sealed class SearchExecutionTests
{
    [Fact]
    public void Request_Succeeds_AndRaisesSearchRequested()
    {
        var result = SearchExecution.Request(TestData.TenantId, TestData.UserId, "Juan", "EMPLOYEE", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.RequestedByUserId.Should().Be(TestData.UserId);
        result.Value.QueryText.Should().Be("Juan");
        result.Value.DomainFilter.Should().Be("EMPLOYEE");
        result.Value.Status.Should().Be(SearchExecutionStatus.Requested);
        result.Value.DomainEvents.OfType<SearchRequested>().Should().ContainSingle();
    }

    [Fact]
    public void Request_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => SearchExecution.Request(Guid.Empty, TestData.UserId, "Juan", null, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Request_Fails_WhenQueryTextIsMissing(string? queryText)
    {
        var result = SearchExecution.Request(TestData.TenantId, TestData.UserId, queryText, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.QueryTextRequired);
    }

    [Fact]
    public void Complete_Succeeds_AndRaisesSearchCompleted()
    {
        var execution = TestData.RequestedExecution();

        var result = execution.Complete(resultCount: 5, latencyMs: 42, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(SearchExecutionStatus.Completed);
        execution.ResultCount.Should().Be(5);
        execution.LatencyMs.Should().Be(42);
        execution.CompletedAtUtc.Should().Be(TestData.NowUtc);
        execution.DomainEvents.OfType<SearchCompleted>().Should().ContainSingle();
    }

    [Fact]
    public void Complete_Fails_WhenAlreadyCompleted()
    {
        var execution = TestData.RequestedExecution();
        execution.Complete(1, 1, TestData.NowUtc);

        var result = execution.Complete(2, 2, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.InvalidSearchExecutionTransition);
    }

    [Fact]
    public void Fail_Succeeds_AndRaisesSearchFailed()
    {
        var execution = TestData.RequestedExecution();

        var result = execution.Fail("Unknown search domain.", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(SearchExecutionStatus.Failed);
        execution.FailureReason.Should().Be("Unknown search domain.");
        execution.DomainEvents.OfType<SearchFailed>().Should().ContainSingle();
    }

    [Fact]
    public void Fail_Fails_WhenReasonIsMissing()
    {
        var execution = TestData.RequestedExecution();

        var result = execution.Fail(" ", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.FailureReasonRequired);
    }

    [Fact]
    public void Fail_Fails_WhenAlreadyCompleted()
    {
        var execution = TestData.RequestedExecution();
        execution.Complete(1, 1, TestData.NowUtc);

        var result = execution.Fail("too late", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.InvalidSearchExecutionTransition);
    }
}
