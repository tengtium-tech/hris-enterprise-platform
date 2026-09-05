using FluentAssertions;
using Hris.Application.Pagination;
using Xunit;

namespace Hris.Application.Tests.Pagination;

public sealed class PageRequestTests
{
    [Fact]
    public void Validate_Succeeds_ForAValidPageAndPageSize()
    {
        var request = new PageRequest(1, 20);

        request.Validate(maxPageSize: 100).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_Succeeds_WhenPageSizeExactlyMeetsTheMaximum()
    {
        var request = new PageRequest(1, 100);

        request.Validate(maxPageSize: 100).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Fails_ForANonPositivePage(int page)
    {
        var request = new PageRequest(page, 20);

        var result = request.Validate(maxPageSize: 100);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaginationErrors.PageMustBePositive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Fails_ForANonPositivePageSize(int pageSize)
    {
        var request = new PageRequest(1, pageSize);

        var result = request.Validate(maxPageSize: 100);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaginationErrors.PageSizeMustBePositive);
    }

    [Fact]
    public void Validate_Fails_WhenPageSizeExceedsTheMaximum_RatherThanSilentlyCapping()
    {
        var request = new PageRequest(1, 101);

        var result = request.Validate(maxPageSize: 100);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaginationErrors.PageSizeExceedsMaximum);
    }
}
