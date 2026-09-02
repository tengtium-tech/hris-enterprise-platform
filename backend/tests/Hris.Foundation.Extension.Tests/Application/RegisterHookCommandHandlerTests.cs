using FluentAssertions;
using Hris.Foundation.Extension.Application.Commands;
using Hris.Foundation.Extension.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

public sealed class RegisterHookCommandHandlerTests
{
    private readonly IExtensionPointRepository _extensionPointRepository = Substitute.For<IExtensionPointRepository>();
    private readonly IHookRepository _hookRepository = Substitute.For<IHookRepository>();
    private readonly RegisterHookCommandHandler _handler;

    public RegisterHookCommandHandlerTests()
    {
        _handler = new RegisterHookCommandHandler(_extensionPointRepository, _hookRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenExtensionPointIsPublishedAndSupportsTheRequestedHookType()
    {
        var extensionPoint = TestData.PublishedExtensionPoint();
        _extensionPointRepository.GetByIdAsync(extensionPoint.Id, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(
            new RegisterHookCommand(extensionPoint.Id.Value, HookType.Before, "Handler.Reference", "Employee"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _hookRepository.Received(1).AddAsync(Arg.Any<Hook>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenExtensionPointDoesNotExist()
    {
        _extensionPointRepository.GetByIdAsync(Arg.Any<ExtensionPointId>(), Arg.Any<CancellationToken>()).Returns((ExtensionPoint?)null);

        var result = await _handler.Handle(
            new RegisterHookCommand(Guid.NewGuid(), HookType.Before, "Handler.Reference", "Employee"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointNotFound);
        await _hookRepository.DidNotReceive().AddAsync(Arg.Any<Hook>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenExtensionPointIsNotPublished()
    {
        var extensionPoint = TestData.RegisteredExtensionPoint();
        _extensionPointRepository.GetByIdAsync(extensionPoint.Id, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(
            new RegisterHookCommand(extensionPoint.Id.Value, HookType.Before, "Handler.Reference", "Employee"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue("only a Published extension point accepts new hook registrations");
        result.Error.Should().Be(ExtensionErrors.ExtensionPointNotPublished);
        await _hookRepository.DidNotReceive().AddAsync(Arg.Any<Hook>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenHandlerReferenceIsEmpty_EvenThoughTheExtensionPointItselfIsValid()
    {
        var extensionPoint = TestData.PublishedExtensionPoint();
        _extensionPointRepository.GetByIdAsync(extensionPoint.Id, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(
            new RegisterHookCommand(extensionPoint.Id.Value, HookType.Before, string.Empty, "Employee"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue(
            "Hook.Register's own validation still applies even once the extension-point-level checks pass");
        result.Error.Should().Be(ExtensionErrors.HandlerReferenceRequired);
        await _hookRepository.DidNotReceive().AddAsync(Arg.Any<Hook>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTheExtensionPointDoesNotSupportTheRequestedHookType()
    {
        var extensionPoint = TestData.RegisteredExtensionPoint(supportedHookTypes: [HookType.After]);
        extensionPoint.Publish(TestData.NowUtc);
        _extensionPointRepository.GetByIdAsync(extensionPoint.Id, Arg.Any<CancellationToken>()).Returns(extensionPoint);

        var result = await _handler.Handle(
            new RegisterHookCommand(extensionPoint.Id.Value, HookType.Before, "Handler.Reference", "Employee"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.HookTypeNotSupportedByExtensionPoint);
        await _hookRepository.DidNotReceive().AddAsync(Arg.Any<Hook>(), Arg.Any<CancellationToken>());
    }
}
