using FluentAssertions;
using Hris.Foundation.Extension.Application.Queries;
using Hris.Foundation.Extension.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

public sealed class GetExtensionPointQueryHandlerTests
{
    private readonly IExtensionPointRepository _repository = Substitute.For<IExtensionPointRepository>();
    private readonly GetExtensionPointQueryHandler _handler;

    public GetExtensionPointQueryHandlerTests()
    {
        _handler = new GetExtensionPointQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheExtensionPoint_WhenItExists()
    {
        var extensionPoint = TestData.RegisteredExtensionPoint();
        _repository.GetByKeyAsync(extensionPoint.Key, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(new GetExtensionPointQuery(extensionPoint.Key.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExtensionPointId.Should().Be(extensionPoint.Id.Value);
        result.Value.Key.Should().Be(extensionPoint.Key.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenExtensionPointDoesNotExist()
    {
        _repository.GetByKeyAsync(Arg.Any<ExtensionPointKey>(), Arg.Any<CancellationToken>()).Returns((ExtensionPoint?)null);

        var result = await _handler.Handle(new GetExtensionPointQuery("employee.before-save"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenKeyIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(new GetExtensionPointQuery(string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointKeyRequired);
        await _repository.DidNotReceive().GetByKeyAsync(Arg.Any<ExtensionPointKey>(), Arg.Any<CancellationToken>());
    }
}
