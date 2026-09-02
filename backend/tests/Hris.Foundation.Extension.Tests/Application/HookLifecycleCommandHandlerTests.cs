using FluentAssertions;
using Hris.Foundation.Extension.Application.Commands;
using Hris.Foundation.Extension.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

public sealed class DisableHookCommandHandlerTests
{
    private readonly IHookRepository _repository = Substitute.For<IHookRepository>();
    private readonly DisableHookCommandHandler _handler;

    public DisableHookCommandHandlerTests()
    {
        _handler = new DisableHookCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenHookExists()
    {
        var hook = TestData.RegisteredHook();
        _repository.GetByIdAsync(hook.Id, Arg.Any<CancellationToken>()).Returns(hook);

        var result = await _handler.Handle(new DisableHookCommand(hook.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        hook.Status.Should().Be(HookStatus.Disabled);
    }

    [Fact]
    public async Task Handle_Fails_WhenHookDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<HookId>(), Arg.Any<CancellationToken>()).Returns((Hook?)null);

        var result = await _handler.Handle(new DisableHookCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.HookNotFound);
    }
}

public sealed class EnableHookCommandHandlerTests
{
    private readonly IHookRepository _repository = Substitute.For<IHookRepository>();
    private readonly EnableHookCommandHandler _handler;

    public EnableHookCommandHandlerTests()
    {
        _handler = new EnableHookCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenHookExists()
    {
        var hook = TestData.DisabledHook();
        _repository.GetByIdAsync(hook.Id, Arg.Any<CancellationToken>()).Returns(hook);

        var result = await _handler.Handle(new EnableHookCommand(hook.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        hook.Status.Should().Be(HookStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenHookDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<HookId>(), Arg.Any<CancellationToken>()).Returns((Hook?)null);

        var result = await _handler.Handle(new EnableHookCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.HookNotFound);
    }
}

public sealed class RemoveHookCommandHandlerTests
{
    private readonly IHookRepository _repository = Substitute.For<IHookRepository>();
    private readonly RemoveHookCommandHandler _handler;

    public RemoveHookCommandHandlerTests()
    {
        _handler = new RemoveHookCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenHookExists()
    {
        var hook = TestData.RegisteredHook();
        _repository.GetByIdAsync(hook.Id, Arg.Any<CancellationToken>()).Returns(hook);

        var result = await _handler.Handle(new RemoveHookCommand(hook.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        hook.Status.Should().Be(HookStatus.Removed);
    }

    [Fact]
    public async Task Handle_Fails_WhenHookDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<HookId>(), Arg.Any<CancellationToken>()).Returns((Hook?)null);

        var result = await _handler.Handle(new RemoveHookCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.HookNotFound);
    }
}
