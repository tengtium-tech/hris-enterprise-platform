using FluentAssertions;
using Hris.Foundation.Numbering.Application.Queries;
using Hris.Foundation.Numbering.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

public sealed class GetIssuedNumberQueryHandlerTests
{
    private readonly IIssuedNumberRepository _repository = Substitute.For<IIssuedNumberRepository>();
    private readonly GetIssuedNumberQueryHandler _handler;

    public GetIssuedNumberQueryHandlerTests()
    {
        _handler = new GetIssuedNumberQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheDto_WhenIssuedNumberExists()
    {
        var issuedNumber = TestData.ReservedNumber();
        _repository.GetByIdAsync(issuedNumber.Id, Arg.Any<CancellationToken>()).Returns(issuedNumber);

        var result = await _handler.Handle(new GetIssuedNumberQuery(issuedNumber.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IssuedNumberId.Should().Be(issuedNumber.Id.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenIssuedNumberDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IssuedNumberId>(), Arg.Any<CancellationToken>()).Returns((IssuedNumber?)null);

        var result = await _handler.Handle(new GetIssuedNumberQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.IssuedNumberNotFound);
    }
}
