using FluentAssertions;
using Hris.Foundation.Tenant.Domain;
using Xunit;

namespace Hris.Foundation.Tenant.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2: "Equality is by value, not
/// reference." These nine records are Domain Events, not Value Objects, but the same
/// expectation applies to any immutable data-carrying type this framework hands to a
/// caller -- each is confirmed here to actually behave as a proper record (value
/// equality, a real <c>ToString</c>), the identical shape
/// <c>AuthorizationEventsTests</c> already establishes.
/// </summary>
public sealed class TenantEventsTests
{
    [Fact]
    public void TenantCreated_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantCreated(
            Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), TestData.NewTenantCode(),
            "ACME", SubscriptionPlan.Growth, TestData.NewPlatformOperatorId());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantCreated));
    }

    [Fact]
    public void TenantProvisioned_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantProvisioned(
            Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), TestData.NewTenantConfigurationId());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantProvisioned));
    }

    [Fact]
    public void TenantActivated_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantActivated(Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), TestData.NewUserAccountId());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantActivated));
    }

    [Fact]
    public void TenantSuspended_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantSuspended(
            Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), TestData.NewPlatformOperatorId(), "Non-payment");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantSuspended));
    }

    [Fact]
    public void TenantReactivated_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantReactivated(Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), TestData.NewPlatformOperatorId());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantReactivated));
    }

    [Fact]
    public void TenantArchived_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantArchived(
            Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), TestData.NewPlatformOperatorId(), "Churned");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantArchived));
    }

    [Fact]
    public void TenantDeleted_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantDeleted(
            Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), TestData.NewPlatformOperatorId(),
            "Retention elapsed", "RA 10173 request");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantDeleted));
    }

    [Fact]
    public void TenantLicenseUpdated_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantLicenseUpdated(
            Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), SubscriptionPlan.Starter,
            SubscriptionPlan.Growth, TestData.NewPlatformOperatorId(), []);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantLicenseUpdated));
    }

    [Fact]
    public void TenantUpdated_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantUpdated(
            Guid.NewGuid(), TestData.NowUtc, new TenantId(Guid.NewGuid()), "Old Name", "New Name", TestData.NewPlatformOperatorId());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantUpdated));
    }
}
