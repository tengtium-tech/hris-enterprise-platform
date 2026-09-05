using FluentAssertions;
using Hris.Application.Pagination;
using Xunit;

namespace Hris.Application.Tests.Pagination;

public sealed class SortRequestTests
{
    [Fact]
    public void Parse_ReturnsEmpty_ForNull()
    {
        SortRequest.Parse(null).Should().BeEmpty();
    }

    [Fact]
    public void Parse_ReturnsEmpty_ForWhitespace()
    {
        SortRequest.Parse("   ").Should().BeEmpty();
    }

    [Fact]
    public void Parse_ReturnsOneAscendingField_ForASingleFieldWithNoPrefix()
    {
        SortRequest.Parse("lastName").Should().BeEquivalentTo(
        [
            new SortField("lastName", Descending: false),
        ]);
    }

    [Fact]
    public void Parse_ReturnsADescendingField_ForALeadingHyphen()
    {
        SortRequest.Parse("-createdAt").Should().BeEquivalentTo(
        [
            new SortField("createdAt", Descending: true),
        ]);
    }

    [Fact]
    public void Parse_ReturnsFieldsInOrder_ForACommaSeparatedList_MixingDescendingAndAscending()
    {
        SortRequest.Parse("-createdAt,lastName").Should().BeEquivalentTo(
        [
            new SortField("createdAt", Descending: true),
            new SortField("lastName", Descending: false),
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void Parse_TrimsWhitespace_AroundEachField()
    {
        SortRequest.Parse(" -createdAt , lastName ").Should().BeEquivalentTo(
        [
            new SortField("createdAt", Descending: true),
            new SortField("lastName", Descending: false),
        ], options => options.WithStrictOrdering());
    }
}
