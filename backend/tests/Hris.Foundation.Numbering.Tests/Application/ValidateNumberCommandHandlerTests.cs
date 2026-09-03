using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class ValidateNumberCommandHandlerTests
{
    private readonly IIssuedNumberRepository _issuedNumberRepository = Substitute.For<IIssuedNumberRepository>();
    private readonly INumberSeriesRepository _numberSeriesRepository = Substitute.For<INumberSeriesRepository>();
    private readonly ValidateNumberCommandHandler _handler;

    public ValidateNumberCommandHandlerTests()
    {
        _handler = new ValidateNumberCommandHandler(_issuedNumberRepository, _numberSeriesRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTheFormattedNumberStillMatchesTheSeriesCurrentFormat()
    {
        var prefix = TestData.NewPrefix("EMP");
        var format = TestData.NewFormat(includeYear: true);
        var series = TestData.RegisteredSeries(prefix: prefix, format: format);

        var formattedNumber = FormattedNumber.Create(format.Format(prefix, 1, TestData.NowUtc)).Value;
        var issuedNumber = TestData.AssignedNumber(series.Id, sequenceValue: 1, formattedNumber: formattedNumber, nowUtc: TestData.NowUtc);

        _issuedNumberRepository.GetByIdAsync(issuedNumber.Id, Arg.Any<CancellationToken>()).Returns(issuedNumber);
        _numberSeriesRepository.GetByIdAsync(series.Id, Arg.Any<CancellationToken>()).Returns(series);

        var result = await _handler.Handle(new ValidateNumberCommand(issuedNumber.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Validated);
    }

    [Fact]
    public async Task Handle_Fails_WhenIssuedNumberDoesNotExist()
    {
        _issuedNumberRepository.GetByIdAsync(Arg.Any<IssuedNumberId>(), Arg.Any<CancellationToken>()).Returns((IssuedNumber?)null);

        var result = await _handler.Handle(new ValidateNumberCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.IssuedNumberNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheOwningSeriesDoesNotExist()
    {
        var issuedNumber = TestData.AssignedNumber();
        _issuedNumberRepository.GetByIdAsync(issuedNumber.Id, Arg.Any<CancellationToken>()).Returns(issuedNumber);
        _numberSeriesRepository.GetByIdAsync(Arg.Any<NumberSeriesId>(), Arg.Any<CancellationToken>()).Returns((NumberSeries?)null);

        var result = await _handler.Handle(new ValidateNumberCommand(issuedNumber.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.NumberSeriesNotFound);
    }
}
