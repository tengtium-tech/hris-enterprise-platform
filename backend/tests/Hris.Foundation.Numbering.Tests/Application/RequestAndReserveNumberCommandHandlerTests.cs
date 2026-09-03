using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class RequestAndReserveNumberCommandHandlerTests
{
    private readonly INumberSeriesRepository _numberSeriesRepository = Substitute.For<INumberSeriesRepository>();
    private readonly IIssuedNumberRepository _issuedNumberRepository = Substitute.For<IIssuedNumberRepository>();
    private readonly RequestAndReserveNumberCommandHandler _handler;

    public RequestAndReserveNumberCommandHandlerTests()
    {
        _handler = new RequestAndReserveNumberCommandHandler(
            _numberSeriesRepository, _issuedNumberRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_ClaimsTheAtomicallyIncrementedValue_AndPersistsTheIssuedNumber()
    {
        var series = TestData.RegisteredSeries(prefix: TestData.NewPrefix("EMP"), format: TestData.NewFormat(includeYear: true));
        _numberSeriesRepository.GetByIdAsync(series.Id, Arg.Any<CancellationToken>()).Returns(series);
        _numberSeriesRepository.IncrementAndGetNextSequenceValueAsync(series.Id, Arg.Any<CancellationToken>()).Returns(1L);

        var result = await _handler.Handle(new RequestAndReserveNumberCommand(series.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        series.CurrentSequenceValue.Should().Be(1, "the handler reconciles the aggregate's own in-memory value after the atomic increment");

        await _issuedNumberRepository.Received(1).AddAsync(
            Arg.Is<IssuedNumber>(number =>
                number.Status == NumberLifecycleStatus.Reserved
                && number.SequenceValue == 1
                && number.FormattedNumber!.Value == "EMP-2026-000001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NeverCallsTheAtomicIncrement_WhenTheSeriesDoesNotExist()
    {
        _numberSeriesRepository.GetByIdAsync(Arg.Any<NumberSeriesId>(), Arg.Any<CancellationToken>()).Returns((NumberSeries?)null);

        var result = await _handler.Handle(new RequestAndReserveNumberCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.NumberSeriesNotFound);
        await _numberSeriesRepository.DidNotReceive().IncrementAndGetNextSequenceValueAsync(Arg.Any<NumberSeriesId>(), Arg.Any<CancellationToken>());
    }
}
