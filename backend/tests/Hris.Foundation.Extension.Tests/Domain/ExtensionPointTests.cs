using FluentAssertions;
using Hris.Foundation.Extension.Domain;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Domain;

public sealed class ExtensionPointTests
{
    [Fact]
    public void Register_Succeeds_WithValidInput()
    {
        var key = TestData.NewKey();

        var result = ExtensionPoint.Register(
            key, "Before Employee Save", "desc", ExtensionPointType.BusinessLogic, "Employee",
            [HookType.Before], TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be(key);
        result.Value.Name.Should().Be("Before Employee Save");
        result.Value.OwningModule.Should().Be("Employee");
        result.Value.Status.Should().Be(ExtensionPointStatus.Draft, "only published extension points should be used");
        result.Value.Version.Should().Be(1);
    }

    [Fact]
    public void Register_TrimsNameDescriptionAndOwningModule()
    {
        var result = ExtensionPoint.Register(
            TestData.NewKey(), "  Before Employee Save  ", "  desc  ", ExtensionPointType.BusinessLogic, "  Employee  ",
            [HookType.Before], TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Before Employee Save");
        result.Value.Description.Should().Be("desc");
        result.Value.OwningModule.Should().Be("Employee");
    }

    [Fact]
    public void Register_AllowsANullDescription()
    {
        var result = ExtensionPoint.Register(
            TestData.NewKey(), "Before Employee Save", null, ExtensionPointType.BusinessLogic, "Employee",
            [HookType.Before], TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Fails_WhenNameIsNullOrWhitespace(string? name)
    {
        var result = ExtensionPoint.Register(
            TestData.NewKey(), name, "desc", ExtensionPointType.BusinessLogic, "Employee",
            [HookType.Before], TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.NameRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Fails_WhenOwningModuleIsNullOrWhitespace(string? owningModule)
    {
        var result = ExtensionPoint.Register(
            TestData.NewKey(), "Before Employee Save", "desc", ExtensionPointType.BusinessLogic, owningModule,
            [HookType.Before], TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.OwningModuleRequired);
    }

    [Fact]
    public void Register_Fails_WhenSupportedHookTypesIsEmpty()
    {
        var result = ExtensionPoint.Register(
            TestData.NewKey(), "Before Employee Save", "desc", ExtensionPointType.BusinessLogic, "Employee",
            [], TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.SupportedHookTypesRequired);
    }

    [Fact]
    public void Register_RaisesExtensionPointRegisteredEvent_WithCorrectData()
    {
        var key = TestData.NewKey();

        var extensionPoint = ExtensionPoint.Register(
            key, "Before Employee Save", "desc", ExtensionPointType.BusinessLogic, "Employee",
            [HookType.Before], TestData.NowUtc).Value;

        extensionPoint.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ExtensionPointRegistered>()
            .Which.Should().BeEquivalentTo(new
            {
                ExtensionPointId = extensionPoint.Id,
                Key = key,
                Name = "Before Employee Save",
                ExtensionPointType = ExtensionPointType.BusinessLogic,
                OwningModule = "Employee",
            });
    }

    [Fact]
    public void Publish_Succeeds_FromDraft()
    {
        var extensionPoint = TestData.RegisteredExtensionPoint();

        var result = extensionPoint.Publish(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        extensionPoint.Status.Should().Be(ExtensionPointStatus.Published);
    }

    [Fact]
    public void Publish_RaisesExtensionPointPublishedEvent_WithTheCurrentVersion()
    {
        var extensionPoint = TestData.RegisteredExtensionPoint();

        extensionPoint.Publish(TestData.NowUtc);

        extensionPoint.DomainEvents.OfType<ExtensionPointPublished>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ExtensionPointId = extensionPoint.Id, Version = 1 });
    }

    [Fact]
    public void Publish_Fails_WhenNotDraft()
    {
        var extensionPoint = TestData.PublishedExtensionPoint();

        var result = extensionPoint.Publish(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.InvalidExtensionPointLifecycleTransition);
    }

    [Fact]
    public void Deprecate_Succeeds_FromPublished()
    {
        var extensionPoint = TestData.PublishedExtensionPoint();

        var result = extensionPoint.Deprecate("Superseded.", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        extensionPoint.Status.Should().Be(ExtensionPointStatus.Deprecated);
    }

    [Fact]
    public void Deprecate_RaisesExtensionPointDeprecatedEvent_WithTheReason()
    {
        var extensionPoint = TestData.PublishedExtensionPoint();

        extensionPoint.Deprecate("Superseded.", TestData.NowUtc);

        extensionPoint.DomainEvents.OfType<ExtensionPointDeprecated>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ExtensionPointId = extensionPoint.Id, Reason = "Superseded." });
    }

    [Fact]
    public void Deprecate_Fails_WhenNotPublished()
    {
        var extensionPoint = TestData.RegisteredExtensionPoint();

        var result = extensionPoint.Deprecate("Superseded.", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.InvalidExtensionPointLifecycleTransition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deprecate_Fails_WhenReasonIsNullOrWhitespace(string? reason)
    {
        var extensionPoint = TestData.PublishedExtensionPoint();

        var result = extensionPoint.Deprecate(reason, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.DeprecationReasonRequired);
    }

    [Fact]
    public void Deprecate_NeverDisablesAnAlreadyRegisteredHook_ItOnlyChangesTheExtensionPointsOwnStatus()
    {
        var extensionPoint = TestData.PublishedExtensionPoint();
        var hook = TestData.RegisteredHook(extensionPoint.Id);

        extensionPoint.Deprecate("Superseded.", TestData.NowUtc);

        hook.Status.Should().Be(HookStatus.Active, "a Deprecated point remains usable by any Hook already registered against it");
    }

    [Fact]
    public void Retire_Succeeds_FromDeprecated()
    {
        var extensionPoint = TestData.DeprecatedExtensionPoint();

        var result = extensionPoint.Retire(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        extensionPoint.Status.Should().Be(ExtensionPointStatus.Retired);
    }

    [Fact]
    public void Retire_RaisesExtensionPointRetiredEvent()
    {
        var extensionPoint = TestData.DeprecatedExtensionPoint();

        extensionPoint.Retire(TestData.NowUtc);

        extensionPoint.DomainEvents.OfType<ExtensionPointRetired>().Should().ContainSingle()
            .Which.ExtensionPointId.Should().Be(extensionPoint.Id);
    }

    [Theory]
    [InlineData(ExtensionPointStatus.Draft)]
    [InlineData(ExtensionPointStatus.Published)]
    public void Retire_Fails_WhenNotDeprecated(ExtensionPointStatus status)
    {
        var extensionPoint = status == ExtensionPointStatus.Draft
            ? TestData.RegisteredExtensionPoint()
            : TestData.PublishedExtensionPoint();

        var result = extensionPoint.Retire(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.InvalidExtensionPointLifecycleTransition);
    }
}
