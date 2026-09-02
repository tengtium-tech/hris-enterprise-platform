using FluentAssertions;
using Hris.Foundation.Extension.Application.Commands;
using Hris.Foundation.Extension.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

public sealed class PublishExtensionPointCommandHandlerTests
{
    private readonly IExtensionPointRepository _repository = Substitute.For<IExtensionPointRepository>();
    private readonly PublishExtensionPointCommandHandler _handler;

    public PublishExtensionPointCommandHandlerTests()
    {
        _handler = new PublishExtensionPointCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenExtensionPointExists()
    {
        var extensionPoint = TestData.RegisteredExtensionPoint();
        _repository.GetByIdAsync(extensionPoint.Id, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(new PublishExtensionPointCommand(extensionPoint.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        extensionPoint.Status.Should().Be(ExtensionPointStatus.Published);
    }

    [Fact]
    public async Task Handle_Fails_WhenExtensionPointDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ExtensionPointId>(), Arg.Any<CancellationToken>()).Returns((ExtensionPoint?)null);

        var result = await _handler.Handle(new PublishExtensionPointCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointNotFound);
    }
}

public sealed class DeprecateExtensionPointCommandHandlerTests
{
    private readonly IExtensionPointRepository _repository = Substitute.For<IExtensionPointRepository>();
    private readonly DeprecateExtensionPointCommandHandler _handler;

    public DeprecateExtensionPointCommandHandlerTests()
    {
        _handler = new DeprecateExtensionPointCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenExtensionPointIsPublished()
    {
        var extensionPoint = TestData.PublishedExtensionPoint();
        _repository.GetByIdAsync(extensionPoint.Id, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(
            new DeprecateExtensionPointCommand(extensionPoint.Id.Value, "Superseded."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        extensionPoint.Status.Should().Be(ExtensionPointStatus.Deprecated);
    }

    [Fact]
    public async Task Handle_Fails_WhenExtensionPointDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ExtensionPointId>(), Arg.Any<CancellationToken>()).Returns((ExtensionPoint?)null);

        var result = await _handler.Handle(new DeprecateExtensionPointCommand(Guid.NewGuid(), "Reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointNotFound);
    }
}

public sealed class RetireExtensionPointCommandHandlerTests
{
    private readonly IExtensionPointRepository _repository = Substitute.For<IExtensionPointRepository>();
    private readonly RetireExtensionPointCommandHandler _handler;

    public RetireExtensionPointCommandHandlerTests()
    {
        _handler = new RetireExtensionPointCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenExtensionPointIsDeprecated()
    {
        var extensionPoint = TestData.DeprecatedExtensionPoint();
        _repository.GetByIdAsync(extensionPoint.Id, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(new RetireExtensionPointCommand(extensionPoint.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        extensionPoint.Status.Should().Be(ExtensionPointStatus.Retired);
    }

    [Fact]
    public async Task Handle_Fails_WhenExtensionPointDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ExtensionPointId>(), Arg.Any<CancellationToken>()).Returns((ExtensionPoint?)null);

        var result = await _handler.Handle(new RetireExtensionPointCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointNotFound);
    }
}
