using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class ConfirmNumberGeneratedCommandHandlerTests
{
    private readonly IIssuedNumberRepository _repository = Substitute.For<IIssuedNumberRepository>();
    private readonly ConfirmNumberGeneratedCommandHandler _handler;

    public ConfirmNumberGeneratedCommandHandlerTests()
    {
        _handler = new ConfirmNumberGeneratedCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenReserved()
    {
        var issuedNumber = TestData.ReservedNumber();
        _repository.GetByIdAsync(issuedNumber.Id, Arg.Any<CancellationToken>()).Returns(issuedNumber);

        var result = await _handler.Handle(new ConfirmNumberGeneratedCommand(issuedNumber.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Generated);
    }

    [Fact]
    public async Task Handle_Fails_WhenIssuedNumberDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IssuedNumberId>(), Arg.Any<CancellationToken>()).Returns((IssuedNumber?)null);

        var result = await _handler.Handle(new ConfirmNumberGeneratedCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.IssuedNumberNotFound);
    }
}
