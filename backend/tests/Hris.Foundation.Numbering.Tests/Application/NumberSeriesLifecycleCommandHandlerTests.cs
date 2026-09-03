using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class UpdateNumberSeriesFormatCommandHandlerTests
{
    private readonly INumberSeriesRepository _repository = Substitute.For<INumberSeriesRepository>();
    private readonly UpdateNumberSeriesFormatCommandHandler _handler;

    public UpdateNumberSeriesFormatCommandHandlerTests()
    {
        _handler = new UpdateNumberSeriesFormatCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenSeriesExists()
    {
        var series = TestData.RegisteredSeries();
        _repository.GetByIdAsync(series.Id, Arg.Any<CancellationToken>()).Returns(series);

        var command = new UpdateNumberSeriesFormatCommand(series.Id.Value, "EEE", 8, false, true, "/", SequenceResetPolicy.Monthly);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        series.Prefix.Value.Should().Be("EEE");
        series.ResetPolicy.Should().Be(SequenceResetPolicy.Monthly);
    }

    [Fact]
    public async Task Handle_Fails_WhenSeriesDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NumberSeriesId>(), Arg.Any<CancellationToken>()).Returns((NumberSeries?)null);

        var result = await _handler.Handle(
            new UpdateNumberSeriesFormatCommand(Guid.NewGuid(), "EMP", 6, true, false, "-", SequenceResetPolicy.Never), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.NumberSeriesNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenPrefixIsInvalid()
    {
        var series = TestData.RegisteredSeries();
        _repository.GetByIdAsync(series.Id, Arg.Any<CancellationToken>()).Returns(series);

        var command = new UpdateNumberSeriesFormatCommand(series.Id.Value, "TOOLONGPREFIX", 6, true, false, "-", SequenceResetPolicy.Never);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.PrefixInvalid);
    }

    [Fact]
    public async Task Handle_Fails_WhenFormatIsInvalid()
    {
        var series = TestData.RegisteredSeries();
        _repository.GetByIdAsync(series.Id, Arg.Any<CancellationToken>()).Returns(series);

        var command = new UpdateNumberSeriesFormatCommand(series.Id.Value, "EMP", 0, true, false, "-", SequenceResetPolicy.Never);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.RunningNumberLengthOutOfRange);
    }
}

public sealed class ResetSequenceCommandHandlerTests
{
    private readonly INumberSeriesRepository _repository = Substitute.For<INumberSeriesRepository>();
    private readonly ResetSequenceCommandHandler _handler;

    public ResetSequenceCommandHandlerTests()
    {
        _handler = new ResetSequenceCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenSeriesExists()
    {
        var series = TestData.SeriesWithSequenceValue(99);
        _repository.GetByIdAsync(series.Id, Arg.Any<CancellationToken>()).Returns(series);

        var result = await _handler.Handle(new ResetSequenceCommand(series.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        series.CurrentSequenceValue.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Fails_WhenSeriesDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<NumberSeriesId>(), Arg.Any<CancellationToken>()).Returns((NumberSeries?)null);

        var result = await _handler.Handle(new ResetSequenceCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.NumberSeriesNotFound);
    }
}
