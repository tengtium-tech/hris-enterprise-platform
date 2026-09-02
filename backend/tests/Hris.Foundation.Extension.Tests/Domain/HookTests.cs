using FluentAssertions;
using Hris.Foundation.Extension.Domain;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Domain;

public sealed class HookTests
{
    [Fact]
    public void Register_Succeeds_WithValidInput()
    {
        var extensionPointId = new ExtensionPointId(Guid.NewGuid());

        var result = Hook.Register(extensionPointId, HookType.Before, "Handler.Reference", "Employee", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExtensionPointId.Should().Be(extensionPointId);
        result.Value.HookType.Should().Be(HookType.Before);
        result.Value.HandlerReference.Should().Be("Handler.Reference");
        result.Value.OwningModule.Should().Be("Employee");
        result.Value.Status.Should().Be(HookStatus.Active);
    }

    [Fact]
    public void Register_TrimsHandlerReferenceAndOwningModule()
    {
        var result = Hook.Register(
            new ExtensionPointId(Guid.NewGuid()), HookType.Before, "  Handler.Reference  ", "  Employee  ", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.HandlerReference.Should().Be("Handler.Reference");
        result.Value.OwningModule.Should().Be("Employee");
    }

    [Fact]
    public void Register_Fails_WhenExtensionPointIdIsDefault()
    {
        var result = Hook.Register(default, HookType.Before, "Handler.Reference", "Employee", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointNotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Fails_WhenHandlerReferenceIsNullOrWhitespace(string? handlerReference)
    {
        var result = Hook.Register(new ExtensionPointId(Guid.NewGuid()), HookType.Before, handlerReference, "Employee", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.HandlerReferenceRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Fails_WhenOwningModuleIsNullOrWhitespace(string? owningModule)
    {
        var result = Hook.Register(new ExtensionPointId(Guid.NewGuid()), HookType.Before, "Handler.Reference", owningModule, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.OwningModuleRequired);
    }

    [Fact]
    public void Register_RaisesHookRegisteredEvent_WithCorrectData()
    {
        var extensionPointId = new ExtensionPointId(Guid.NewGuid());

        var hook = Hook.Register(extensionPointId, HookType.After, "Handler.Reference", "Employee", TestData.NowUtc).Value;

        hook.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<HookRegistered>()
            .Which.Should().BeEquivalentTo(new
            {
                HookId = hook.Id,
                ExtensionPointId = extensionPointId,
                HookType = HookType.After,
                HandlerReference = "Handler.Reference",
                OwningModule = "Employee",
            });
    }

    [Fact]
    public void Disable_Succeeds_FromActive()
    {
        var hook = TestData.RegisteredHook();

        var result = hook.Disable(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        hook.Status.Should().Be(HookStatus.Disabled);
    }

    [Fact]
    public void Disable_RaisesHookDisabledEvent()
    {
        var hook = TestData.RegisteredHook();

        hook.Disable(TestData.NowUtc);

        hook.DomainEvents.OfType<HookDisabled>().Should().ContainSingle().Which.HookId.Should().Be(hook.Id);
    }

    [Fact]
    public void Disable_Fails_WhenNotActive()
    {
        var hook = TestData.DisabledHook();

        var result = hook.Disable(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.InvalidHookLifecycleTransition);
    }

    [Fact]
    public void Enable_Succeeds_FromDisabled()
    {
        var hook = TestData.DisabledHook();

        var result = hook.Enable(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        hook.Status.Should().Be(HookStatus.Active);
    }

    [Fact]
    public void Enable_RaisesHookEnabledEvent()
    {
        var hook = TestData.DisabledHook();

        hook.Enable(TestData.NowUtc);

        hook.DomainEvents.OfType<HookEnabled>().Should().ContainSingle().Which.HookId.Should().Be(hook.Id);
    }

    [Fact]
    public void Enable_Fails_WhenNotDisabled()
    {
        var hook = TestData.RegisteredHook();

        var result = hook.Enable(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.InvalidHookLifecycleTransition);
    }

    [Fact]
    public void Remove_Succeeds_FromActive()
    {
        var hook = TestData.RegisteredHook();

        var result = hook.Remove(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        hook.Status.Should().Be(HookStatus.Removed);
    }

    [Fact]
    public void Remove_Succeeds_FromDisabled()
    {
        var hook = TestData.DisabledHook();

        var result = hook.Remove(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        hook.Status.Should().Be(HookStatus.Removed);
    }

    [Fact]
    public void Remove_RaisesHookRemovedEvent()
    {
        var hook = TestData.RegisteredHook();

        hook.Remove(TestData.NowUtc);

        hook.DomainEvents.OfType<HookRemoved>().Should().ContainSingle().Which.HookId.Should().Be(hook.Id);
    }

    [Fact]
    public void Remove_IsTerminal_AndFailsWhenAlreadyRemoved()
    {
        var hook = TestData.RemovedHook();

        var result = hook.Remove(TestData.NowUtc);

        result.IsFailure.Should().BeTrue("a removed Hook is never re-enabled; a handler that wants to subscribe again registers a new Hook");
        result.Error.Should().Be(ExtensionErrors.InvalidHookLifecycleTransition);
    }
}
