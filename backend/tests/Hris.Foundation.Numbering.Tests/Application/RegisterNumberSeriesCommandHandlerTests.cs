using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class RegisterNumberSeriesCommandHandlerTests
{
    private readonly INumberSeriesRepository _repository = Substitute.For<INumberSeriesRepository>();
    private readonly RegisterNumberSeriesCommandHandler _handler;

    public RegisterNumberSeriesCommandHandlerTests()
    {
        _handler = new RegisterNumberSeriesCommandHandler(_repository);
    }

    private static RegisterNumberSeriesCommand ValidCommand(string key = "employee-numbers") =>
        new(key, "EMP", 6, true, false, "-", SequenceResetPolicy.Never);

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewSeries_WhenKeyIsAvailable()
    {
        _repository.ExistsByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<NumberSeries>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenKeyIsAlreadyRegistered()
    {
        _repository.ExistsByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.SeriesKeyAlreadyRegistered);
        await _repository.DidNotReceive().AddAsync(Arg.Any<NumberSeries>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenKeyIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand(key: string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.SeriesKeyRequired);
        await _repository.DidNotReceive().ExistsByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenPrefixIsInvalid()
    {
        _repository.ExistsByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = ValidCommand() with { Prefix = "TOOLONGPREFIX" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.PrefixInvalid);
    }

    [Fact]
    public async Task Handle_Fails_WhenFormatIsInvalid()
    {
        _repository.ExistsByKeyAsync(Arg.Any<SeriesKey>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = ValidCommand() with { RunningNumberLength = 0 };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.RunningNumberLengthOutOfRange);
    }
}
