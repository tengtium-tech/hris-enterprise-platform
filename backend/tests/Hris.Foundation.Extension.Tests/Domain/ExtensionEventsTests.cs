using FluentAssertions;
using Hris.Foundation.Extension.Domain;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2: "Equality is by value, not
/// reference." These eight records are Domain Events, not Value Objects, but the same
/// expectation applies to any immutable data-carrying type this framework hands to a
/// caller -- the identical shape TenantEventsTests already establishes.
/// </summary>
public sealed class ExtensionEventsTests
{
    [Fact]
    public void ExtensionPointRegistered_HasValueEquality_AndAUsefulToString()
    {
        var original = new ExtensionPointRegistered(
            Guid.NewGuid(), TestData.NowUtc, new ExtensionPointId(Guid.NewGuid()), TestData.NewKey(),
            "Before Employee Save", ExtensionPointType.BusinessLogic, "Employee");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ExtensionPointRegistered));
    }

    [Fact]
    public void ExtensionPointPublished_HasValueEquality_AndAUsefulToString()
    {
        var original = new ExtensionPointPublished(Guid.NewGuid(), TestData.NowUtc, new ExtensionPointId(Guid.NewGuid()), 1);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ExtensionPointPublished));
    }

    [Fact]
    public void ExtensionPointDeprecated_HasValueEquality_AndAUsefulToString()
    {
        var original = new ExtensionPointDeprecated(Guid.NewGuid(), TestData.NowUtc, new ExtensionPointId(Guid.NewGuid()), "Superseded.");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ExtensionPointDeprecated));
    }

    [Fact]
    public void ExtensionPointRetired_HasValueEquality_AndAUsefulToString()
    {
        var original = new ExtensionPointRetired(Guid.NewGuid(), TestData.NowUtc, new ExtensionPointId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ExtensionPointRetired));
    }

    [Fact]
    public void HookRegistered_HasValueEquality_AndAUsefulToString()
    {
        var original = new HookRegistered(
            Guid.NewGuid(), TestData.NowUtc, new HookId(Guid.NewGuid()), new ExtensionPointId(Guid.NewGuid()),
            HookType.Before, "Handler.Reference", "Employee");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(HookRegistered));
    }

    [Fact]
    public void HookDisabled_HasValueEquality_AndAUsefulToString()
    {
        var original = new HookDisabled(Guid.NewGuid(), TestData.NowUtc, new HookId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(HookDisabled));
    }

    [Fact]
    public void HookEnabled_HasValueEquality_AndAUsefulToString()
    {
        var original = new HookEnabled(Guid.NewGuid(), TestData.NowUtc, new HookId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(HookEnabled));
    }

    [Fact]
    public void HookRemoved_HasValueEquality_AndAUsefulToString()
    {
        var original = new HookRemoved(Guid.NewGuid(), TestData.NowUtc, new HookId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(HookRemoved));
    }
}
