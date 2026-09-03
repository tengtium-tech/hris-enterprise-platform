using FluentAssertions;
using Hris.Foundation.Numbering.Application.Queries;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class ListIssuedNumbersForSeriesQueryHandlerTests
{
    private readonly IIssuedNumberRepository _repository = Substitute.For<IIssuedNumberRepository>();
    private readonly ListIssuedNumbersForSeriesQueryHandler _handler;

    public ListIssuedNumbersForSeriesQueryHandlerTests()
    {
        _handler = new ListIssuedNumbersForSeriesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryIssuedNumberForTheSeries()
    {
        var seriesId = new NumberSeriesId(Guid.NewGuid());
        IReadOnlyCollection<IssuedNumber> numbers = [TestData.RequestedNumber(seriesId), TestData.ReservedNumber(seriesId)];
        _repository.GetBySeriesIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(numbers);

        var result = await _handler.Handle(new ListIssuedNumbersForSeriesQuery(seriesId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyCollection_WhenNoNumbersExist()
    {
        _repository.GetBySeriesIdAsync(Arg.Any<NumberSeriesId>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<IssuedNumber>)[]);

        var result = await _handler.Handle(new ListIssuedNumbersForSeriesQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
