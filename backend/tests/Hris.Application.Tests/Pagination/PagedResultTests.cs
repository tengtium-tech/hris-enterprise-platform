using FluentAssertions;
using Hris.Application.Pagination;
using Xunit;

namespace Hris.Application.Tests.Pagination;

public sealed class PagedResultTests
{
    [Fact]
    public void TotalPages_RoundsUp_WhenTotalCountDoesNotDivideEvenly()
    {
        var result = new PagedResult<int>(Items: [1, 2, 3], Page: 1, PageSize: 20, TotalCount: 45);

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_DividesEvenly_WhenTotalCountIsAnExactMultipleOfPageSize()
    {
        var result = new PagedResult<int>(Items: [1, 2, 3], Page: 1, PageSize: 20, TotalCount: 40);

        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void TotalPages_IsZero_WhenTotalCountIsZero()
    {
        var result = new PagedResult<int>(Items: [], Page: 1, PageSize: 20, TotalCount: 0);

        result.TotalPages.Should().Be(0);
    }
}
