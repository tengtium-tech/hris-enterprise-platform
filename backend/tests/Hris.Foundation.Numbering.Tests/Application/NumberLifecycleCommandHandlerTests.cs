using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class ReleaseNumberCommandHandlerTests
{
    private readonly IIssuedNumberRepository _repository = Substitute.For<IIssuedNumberRepository>();
    private readonly ReleaseNumberCommandHandler _handler;

    public ReleaseNumberCommandHandlerTests()
    {
        _handler = new ReleaseNumberCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenReleasable()
    {
        var issuedNumber = TestData.RequestedNumber();
        _repository.GetByIdAsync(issuedNumber.Id, Arg.Any<CancellationToken>()).Returns(issuedNumber);

        var result = await _handler.Handle(new ReleaseNumberCommand(issuedNumber.Id.Value, "Abandoned"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Released);
    }

    [Fact]
    public async Task Handle_Fails_WhenIssuedNumberDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IssuedNumberId>(), Arg.Any<CancellationToken>()).Returns((IssuedNumber?)null);

        var result = await _handler.Handle(new ReleaseNumberCommand(Guid.NewGuid(), "Abandoned"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.IssuedNumberNotFound);
    }
}

public sealed class ArchiveNumberCommandHandlerTests
{
    private readonly IIssuedNumberRepository _repository = Substitute.For<IIssuedNumberRepository>();
    private readonly ArchiveNumberCommandHandler _handler;

    public ArchiveNumberCommandHandlerTests()
    {
        _handler = new ArchiveNumberCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenValidated()
    {
        var prefix = TestData.NewPrefix();
        var format = TestData.NewFormat();
        var formattedNumber = FormattedNumber.Create(format.Format(prefix, 1, TestData.NowUtc)).Value;
        var issuedNumber = TestData.AssignedNumber(sequenceValue: 1, formattedNumber: formattedNumber, nowUtc: TestData.NowUtc);
        issuedNumber.Validate(prefix, format, TestData.NowUtc);

        _repository.GetByIdAsync(issuedNumber.Id, Arg.Any<CancellationToken>()).Returns(issuedNumber);

        var result = await _handler.Handle(new ArchiveNumberCommand(issuedNumber.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Archived);
    }

    [Fact]
    public async Task Handle_Fails_WhenIssuedNumberDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IssuedNumberId>(), Arg.Any<CancellationToken>()).Returns((IssuedNumber?)null);

        var result = await _handler.Handle(new ArchiveNumberCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.IssuedNumberNotFound);
    }
}
