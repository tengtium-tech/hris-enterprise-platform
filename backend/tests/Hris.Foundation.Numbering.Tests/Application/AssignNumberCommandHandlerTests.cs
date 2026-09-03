using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class AssignNumberCommandHandlerTests
{
    private readonly IIssuedNumberRepository _repository = Substitute.For<IIssuedNumberRepository>();
    private readonly AssignNumberCommandHandler _handler;

    public AssignNumberCommandHandlerTests()
    {
        _handler = new AssignNumberCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenGenerated()
    {
        var issuedNumber = TestData.GeneratedNumber();
        _repository.GetByIdAsync(issuedNumber.Id, Arg.Any<CancellationToken>()).Returns(issuedNumber);

        var result = await _handler.Handle(new AssignNumberCommand(issuedNumber.Id.Value, "Employee", "EMP-0001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Assigned);
        issuedNumber.AssignedToReferenceId.Should().Be("EMP-0001");
    }

    [Fact]
    public async Task Handle_Fails_WhenIssuedNumberDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IssuedNumberId>(), Arg.Any<CancellationToken>()).Returns((IssuedNumber?)null);

        var result = await _handler.Handle(new AssignNumberCommand(Guid.NewGuid(), "Employee", "EMP-0001"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.IssuedNumberNotFound);
    }
}
