using FluentAssertions;
using Hris.Foundation.Numbering.Application.Queries;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class GetNumberSeriesQueryHandlerTests
{
    private readonly INumberSeriesRepository _repository = Substitute.For<INumberSeriesRepository>();
    private readonly GetNumberSeriesQueryHandler _handler;

    public GetNumberSeriesQueryHandlerTests()
    {
        _handler = new GetNumberSeriesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheDto_WhenSeriesExists()
    {
        var series = TestData.RegisteredSeries(key: TestData.NewSeriesKey("employee-numbers"));
        _repository.GetByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>()).Returns(series);

        var result = await _handler.Handle(new GetNumberSeriesQuery("employee-numbers"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("employee-numbers");
    }

    [Fact]
    public async Task Handle_Fails_WhenSeriesDoesNotExist()
    {
        _repository.GetByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>()).Returns((NumberSeries?)null);

        var result = await _handler.Handle(new GetNumberSeriesQuery("employee-numbers"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.NumberSeriesNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenKeyIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(new GetNumberSeriesQuery(string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.SeriesKeyRequired);
        await _repository.DidNotReceive().GetByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>());
    }
}
